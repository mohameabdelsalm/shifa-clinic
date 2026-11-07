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
| 5 — Scheduling | 🟡 Design done (ADR-008); implementation next |
| 6–10 | Not started (see `implementation-roadmap.md`) |

## Current phase

**Phase 5 — Scheduling & Booking** (availability, slot reservation, booking, cancel, reschedule, check-in, queue, no-show, idempotency). Model: **opus / high** for the concurrency design, then sonnet / medium, then opus / high review.

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

## Tests

`dotnet test --solution Shifa.slnx` → **90 passed, 0 failed** (2026-11-07). Phase 4 mutation checks: with portal tokens allowed on staff endpoints, or any OTP accepted, the portal tests fail. Phase 3 mutation check: with `HasAllBranches` forced to true, the two branch-scope tests fail. Mutation check: with the `Tenant` query filter replaced by `true`, 8 isolation tests fail (then restored).

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

## Next tasks (Phase 5)

Design is fixed in `docs/decisions/ADR-008-Booking-Concurrency.md`; the ordered task list is "Phase 5 detail" in `project/implementation-roadmap.md` (steps 1–8). Implement on **sonnet / medium**; run step 8 (review) on **opus / high**.

Open item to decide during Phase 5: the staff-listing branch scope (see Known gaps), together with "a branch-limited member only books and sees appointments of their branches".
