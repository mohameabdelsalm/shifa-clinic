# Progress

_Last updated: 2026-11-07_

## Phases

| Phase | State |
|---|---|
| 0 — Architecture finalization | ✅ Done |
| 1 — Solution foundation | ✅ Done |
| 2 — Identity, Tenancy, Platform Admin | ✅ Done, security-reviewed (opus / high) and fixed |
| 3 — Clinic setup | ✅ Done |
| 4 — Patients | ✅ Done |
| 5 — Scheduling | ✅ Implemented (ADR-008); review (step 8, opus / high) pending |
| 6–10 | Not started (see `implementation-roadmap.md`) |

## Current phase

**Phase 5 — Scheduling & Booking**: implementation done and tested; next is the step 8 review on **opus / high** (concurrency, idempotency, branch scope, portal rules), then Phase 6.

## Phase 2 security review (2026-11-07, opus / high)

Scope: every anonymous endpoint, every read/write by id, token issue/refresh/revoke, tenant resolution, role/staff management, onboarding. No raw SQL or `IgnoreQueryFilters` exists in `src`.

| # | Severity | Finding | Fix | Test |
|---|---|---|---|---|
| R1 | High | A member with `Staff.Manage` could suspend or demote anyone, including the TenantAdmin | Can only change/suspend members whose current permissions and branches you hold | `A_staff_manager_cannot_suspend_or_demote_someone_more_powerful` |
| R2 | High | The last TenantAdmin could lose the role, leaving the tenant without an admin | Last active TenantAdmin can't be suspended or demoted | `The_last_active_TenantAdmin_...` |
| R3 | Medium | A `Roles.Manage` holder could strip or delete permissions they don't hold | Editing/deleting a role requires holding its current permissions | `A_role_manager_cannot_strip_...` |
| R4 | Medium | Two concurrent refreshes with one token could both succeed (bypassing reuse detection) | Atomic conditional claim (`ExecuteUpdate ... WHERE RevokedAt IS NULL`) | `Two_concurrent_refreshes_...` |
| R5 | Medium | Reactivating a suspended member bypassed `MaxEmployees`; a disabled limit meant unlimited | Capacity check on activate; disabled limit = none allowed | `Reactivating_a_member_...` |
| R6 | Medium | Tenant slug index ignored soft delete, so a failed onboarding made the slug unusable (500 on retry) | Filtered unique index + migration `FilterTenantSlugIndex` | `A_slug_freed_by_a_removed_tenant_...` |
| R7 | Low | No fallback policy: a future endpoint without an attribute would be public | `FallbackPolicy = RequireAuthenticatedUser`; health checks `AllowAnonymous` | existing health test |
| R8 | Low | JWT accepted any HMAC algorithm; 401/403 bodies weren't in the `Response` shape | `ValidAlgorithms = HS256`, `RequireSignedTokens`; JwtBearer `OnChallenge`/`OnForbidden` | `Authentication_and_authorization_failures_use_the_Response_shape` |

Verification: with the `src` fixes stashed, 6 of the 7 regression tests failed (the slug test passed only because the test database already had the new index).

Accepted risks (documented, not fixed now):
- Adding staff by email reveals whether that email already has a Shifa account (an invitation flow would remove this; planned with password reset).
- Anyone can lock an account for 15 minutes with 5 wrong passwords (rate limit 10/min per IP slows it; CAPTCHA/backoff later).
- Rate limit is per client IP; behind a reverse proxy, forwarded headers must be configured (Phase 10).
- Membership/tenant status cache is per instance (30 s): with several instances a suspension can take up to 30 s to apply everywhere.
- Concurrent staff additions can exceed `MaxEmployees` by one (commercial limit, not a safety rule).
- Onboarding writes two schemas without a shared transaction; failure is compensated by soft-deleting the tenant.
- Staff listing (`api/staff`) is still tenant-wide, not limited by the caller's branches: a branch-limited member with `Staff.Read` sees every member of the organization. Decide and implement together with branch-aware staff management (Phase 4 or a follow-up).
- No CORS or security headers yet (Phase 10).

## Done in Phase 0–1

See git history. Highlights: ADR-001..007, architecture docs, `ModuleDbContext` (schema, HiLo, `Tenant`/`SoftDelete` filters, `(TenantId, Id)` keys), `AuditTenantInterceptor`, `GlobalExceptionHandler`, validation filter, health checks, Serilog, Tenancy + Clinic initial migrations.

## Done in Phase 5

- **Scheduling module** (`scheduling` schema): `Appointment` (status machine, `rowversion`), `AppointmentSlot` (5-minute UTC ticks, unique `(TenantId, DoctorId, SlotStart)` over live rows = the double-booking guard at every branch), `AppointmentStatusHistory`, `IdempotencyRecord` (24 h).
- **Availability:** pure `AvailabilityCalculator` (schedule windows in branch time, multi-slot services fit inside one window, DST gaps skipped, repeated hour = first occurrence, time off + the doctor's appointments at any branch are busy, 90-day horizon, nothing in the past).
- **`api/appointments`:** availability, book (`Idempotency-Key` required: same body → same id, different body → 422, concurrent retries → same id), search (≤ 62 days), view, confirm, check-in (from 2 h before until the end of the local day, queue number per branch/doctor/day), cancel (frees the ticks), no-show (30 min after start), reschedule (can overlap its own old ticks, one transaction). Every change writes a history row and an audit entry. Branch-limited staff only see and change appointments of their branches (others are 404).
- **`api/portal/appointments`:** availability, book (source Online, only linked patients with `CanBook`), list of linked patients' appointments, cancel (until 3 h before). Online bookings of a patient with 3+ no-shows start as Pending.
- **Contracts:** `IClinicApi.GetBookingInputsAsync`, `GetBranchTimeZoneAsync`, `GetDoctorTimeOffAsync`; Scheduling events records (not published yet: no outbox before Phase 6/8). Schedule times must sit on the 5-minute grid.
- **Shared fixes found while testing:**
  - JWT lifetime is checked against the app's `TimeProvider` (the same clock that issues tokens).
  - `DatabaseLocks.AcquireAsync` (`sp_getapplock`) serializes "max + 1" numbering. Used for queue numbers and **patient MRNs**: the Phase 4 test `Concurrent_registrations_all_succeed_with_distinct_MRNs` failed 6/6 when run alone (8 concurrent registrations used up the 5 retries → 500). It passed earlier only by timing.
  - `DbUpdateConcurrencyException` → 409.

## Done in Phase 4

- **Patients module:** `Patient` (running number + `MRN-000123` per tenant, retry on a number collision), `PatientIdentifier` (national ID / passport, unique per tenant and type), `PatientGuardian`, `PatientAccountAccess` (phone-number links), `PatientConsent` (grant once, revoke, history kept); `api/patients` (search by name/MRN/phone/exact identifier, view (audited), add, update, guardians, consents, portal-access links).
- **Rules:** under-18 needs a guardian and keeps at least one; duplicate identifiers refused; phone numbers normalized (`PhoneNumbers`, Egypt default); future birth dates refused.
- **Portal:** `api/portal/auth/otp/request|verify` (same answer for unknown numbers/clinics, SMS only to linked numbers, 3 per 10 min, 5-minute single-use code, 5 tries, HMAC-hashed), portal token (tenant + verified phone, no membership), refresh for portal sessions, `api/portal/patients` (only linked patients, separate DTO). Portal tokens are rejected by every staff endpoint (default + fallback policy), and staff tokens by `[PortalUser]`.
- **Shared:** `ISmsSender` + in-memory fake (`GET /dev/sms` in Development), `IAccessContext.IsPortalUser/PortalPhoneNumber`, `IIdentityApi.IsActiveUserAsync`, `IPatientsApi`.

## Done in Phase 3

- **Clinic module:** `Specialty`, `Doctor`, `DoctorSpecialty`, `DoctorBranch`, `DoctorSchedule`, `DoctorTimeOff`, `Service`, `ServicePrice` (all with composite tenant FKs); `api/branches`, `api/specialties`, `api/doctors` (search/view/add/update/status, `PUT schedule`, `POST|DELETE time-off`), `api/services` (CRUD, `PUT price`).
- **Branch scope:** members limited to some branches only see those branches, doctors who work there, and prices for them; they can't add a doctor to another branch, set schedules or prices elsewhere, create branches, or deactivate a doctor who also works elsewhere. `IAccessContext.BranchIds` added.
- **Plan limits:** `MaxBranches`, `MaxDoctors` (disabled entitlement = none allowed).
- **Schedules:** end after start, whole slots, no overlap inside the request or across the doctor's other branches; replacing a schedule is atomic; removing a doctor from a branch drops its schedule.
- **Prices:** history kept, one open price per service and branch (filtered unique index), same price = no-op.
- **Contract:** `IClinicApi.GetExistingBranchIdsAsync` (used by Staff).

## Done in Phase 2

- **Auth plumbing:** JWT (15 min) + rotating refresh tokens with theft detection, `Jwt` settings + dev-only ephemeral key, `[Permission]` / `[PlatformAdmin]` policies, scoped set-once `IAccessContext`, `ITenantScopeFactory`, `IAuditService`, `Permissions` catalog + default roles, `PagedList`, rate limiter on `api/auth`, Swagger Bearer scheme.
- **Identity module:** `User`, `Membership`, `Role`, `RolePermission`, `MembershipRole`, `MembershipBranch`, `RefreshToken`, `AuditLog`; `api/auth` (login, refresh, switch-tenant, logout), `api/roles`, `api/staff`; lockout; escalation guards; `IIdentityApi`; Platform Admin seed command.
- **Tenancy module:** `Plan`, `PlanEntitlement`, `TenantSubscription`, `SubscriptionPayment`; `api/platform/plans`, `api/platform/tenants` (create + onboarding with default roles and first TenantAdmin, suspend, activate, change plan, record payment); `ITenancyApi` (status, entitlements).
- **Clinic:** `IClinicApi.GetExistingBranchIdsAsync`.
- **TenantResolutionHandler:** validates tenant + membership per request (30 s cache) before setting the tenant and access contexts.

## Database migrations

| Module | Migration | Applied to |
|---|---|---|
| Tenancy | `InitialCreate`, `AddPlansAndSubscriptions`, `FilterTenantSlugIndex` | `Shifa` (dev), `Shifa_Tests` |
| Clinic | `InitialCreate`, `AddClinicSetup` (9 composite tenant FKs) | `Shifa` (dev), `Shifa_Tests` |
| Identity | `InitialCreate`, `AddOtpAndPortalTokens` (OtpChallenges, RefreshTokens.IsPortal) | `Shifa` (dev), `Shifa_Tests` |
| Patients | `InitialCreate` (5 tables, 4 composite tenant FKs) | `Shifa` (dev), `Shifa_Tests` |
| Scheduling | `InitialCreate` (4 tables, 4 filtered unique indexes, 2 composite tenant FKs) | `Shifa` (dev), `Shifa_Tests` |

## Tests

`dotnet test --solution Shifa.slnx` → **135 passed, 0 failed** (2026-11-07): unit 30 (new project `Shifa.UnitTests`), architecture 12, integration 93.

Phase 5 (15 integration tests on a fake clock + 30 unit tests): 10 parallel bookings of one slot → exactly one; overlapping multi-slot bookings in parallel → one; idempotency (missing key, replay, reuse 422, 5 parallel retries → one appointment, key expiry); cancel frees the slot; reschedule onto own ticks; one visit per patient/doctor/day; doctor busy across branches; check-in window, queue numbers, no-show timing; parallel check-ins get 1..5; check-in vs cancel race has one winner; branch scope; tenant isolation; portal `CanBook`, 3-hour cancel rule, Pending after 3 no-shows. Calculator: multi-slot, window fit, busy edges, range, DST spring/fall, 23/25-hour days.
**Mutation check:** with `IX_AppointmentSlots_Tick` dropped from `Shifa_Tests`, both race tests failed (5–10 double bookings succeeded); index restored.

Earlier: Phase 4 mutation checks: with portal tokens allowed on staff endpoints, or any OTP accepted, the portal tests fail. Phase 3 mutation check: with `HasAllBranches` forced to true, the two branch-scope tests fail. Mutation check: with the `Tenant` query filter replaced by `true`, 8 isolation tests fail (then restored).

- Architecture (12) and Phase 1 integration (17), unchanged.
- API integration (27), all through HTTP against SQL Server:
  - Authentication: tokens, identical 401s for 5 failure kinds, lockout, refresh rotation + family revocation, logout, one person in two tenants + switch, bad tokens, platform token has no tenant permissions.
  - **Tenant isolation:** each tenant sees only its roles; B can't read/update/delete A's roles; B can't read/update/suspend A's staff; A can't assign B's roles; membership lookup never crosses tenants; suspended tenant locked out immediately (and a stale token doesn't block signing in elsewhere).
  - Authorization: 403 without permission, staff-manager and role-manager escalation blocked, system roles protected, suspended member loses access at once, can't suspend self, plan employee limit, unknown branch ids, platform endpoints reject anonymous and tenant admins, onboarding validation, audit rows for platform actions, tenant list.

## Known gaps / notes

- Password reset/change, subscription-expiry enforcement (assumption A6), JWT key rotation and Platform Admin usage monitoring are deferred (ADR-007).
- No branch endpoints yet, so branch-scope assignment is only tested for rejection of unknown ids; positive branch-scope tests come with Phase 3.
- `AuditService` saves through the request's `IdentityDbContext`; an audit failure after a business change surfaces as an error to the caller (change already committed).
- EF tools 10.0.7 older than runtime 10.0.14 (warning): `dotnet tool update -g dotnet-ef`.
- OpenTelemetry deferred to Phase 10. Docker not needed yet.

- Scheduling: appointment events are defined but not published (needs the outbox, Phase 6/8). Soft-deleted tick rows stay in `AppointmentSlots` (filtered out of the unique index); a cleanup job can come later.
- Scheduling: booking rules (90-day horizon, 3 h, 2 h, 30 min, 3 no-shows) are constants in `SchedulingRules`, to move to tenant settings.

## Next tasks

1. Phase 5 step 8: review on **opus / high** (concurrency, idempotency, branch scope, portal rules, DST).
2. Phase 6 per `project/implementation-roadmap.md`.

Decided in Phase 5: appointments follow branch scope (a branch-limited member only books and sees appointments of their branches). Still open: the staff listing (`api/staff`) is tenant-wide (see accepted risks).
