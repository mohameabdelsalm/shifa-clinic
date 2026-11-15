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
| 5 — Scheduling | ✅ Done, reviewed (opus / high) and fixed |
| HRM layout | ✅ Restructured like Trio HRM (ADR-009) |
| 6 — Clinical / EMR | ✅ Done, reviewed (opus / high) and fixed |
| 7 — Billing | 🟡 Design done (ADR-011); implementation next |
| 8–10 | Not started (see `implementation-roadmap.md`) |

## Current phase

**Phase 7 — Billing: design done** (ADR-011, decisions by the product owner on 2026-11-16: pay on arrival — the invoice is issued at check-in; no discounts for now; cash, card and wallet recorded manually; cashier shifts mandatory). Next: implement the 8 steps in "Phase 7 detail" of `implementation-roadmap.md`, explaining each step in chat while implementing.

## Phase 6 review (2026-11-07, opus / high)

My own read plus an independent reviewer that didn't see my conclusions. **No High findings**: visit authorization (only the appointment's linked doctor), branch scope, tenant isolation, completed-visit immutability (rowversion on every content write), portal isolation and upload path safety were confirmed.

| # | Severity | Finding | Fix |
|---|---|---|---|
| C1 | Medium | If the audit write failed after an attachment row was committed, the cleanup deleted the stored file → a record pointing at a missing file | The file is deleted only when the row wasn't saved; audit runs after, separately |
| C2 | Medium | Re-linking a doctor's staff account while the doctor had open visits handed those visits to another person (e.g. an admin could link themselves and write as Dr. A) | Link/unlink refused while the doctor has open visits (`IClinicalApi.HasOpenVisitsAsync`). Self-link stays allowed (owner-doctor case) and audited |
| C3 | Low-Med | Clinical audit rows used the request's cancellation token: a client disconnect after a commit lost the audit row | Clinical audits use `CancellationToken.None` |
| C4 | Low | Deactivating a doctor mid-visit left visits open for ever | Deactivation refused while the doctor has open visits |
| C5 | Low | Portal prescription reads weren't audited | One audit row per patient returned |
| C6 | Low | `items: [null]` in diagnoses/prescription → 500 | Validators refuse null items (400) |
| C7 | Low | Duplicate ICD codes were silently de-duplicated and could drop the primary flag | Duplicates refused (400) |
| C8 | Low | A cancelled upload could leave a half-written file | Storage deletes partial files; the save is inside the cleanup block |
| C9 | Low | Original file names kept control characters/quotes; no `nosniff` on downloads | Names sanitized; downloads send `X-Content-Type-Options: nosniff` |
| C10 | Low | Unknown/foreign patient id on allergy/condition add returned 400 | 404, per the tenant rule |
| C11 | Low | Visit searches without a patient filter weren't audited (who saw which doctor when) | Every search is audited with its filters (ids only) |
| C12 | Low (mine) | ICD-10 format refused valid U codes (e.g. U07.1 COVID-19) | Pattern `^[A-Z][0-9][0-9A-Z](\.[0-9A-Z]{1,4})?$` |

Verification: build clean; the HTTP smoke run on `Shifa_Smoke` now has **56/56 checks passing**, including new checks for C2, C4, C6, C7, C9, C10, C12. Audit rows for portal reads and searches were confirmed in the database. Not covered by the run: audit failure after commit (C1/C3) and cancelled uploads (C8), which were checked by reading the code only.

Accepted: retrying "add vitals" after a lost response can create a duplicate vitals row (no idempotency key on clinical writes).

## Done in Phase 6 (2026-11-07)

Decisions by the product owner: clinical history follows branch scope (allergies/conditions tenant-wide); doctors **and nurses** record vitals (new permission `Clinical.Vitals`); attachments now, on local disk. Details in ADR-010.

- **Clinic:** `Doctor.MembershipId` links a doctor to a staff account (`PUT api/doctors/account`, unique per tenant). `IClinicApi.GetDoctorIdForMembershipAsync`, `IIdentityApi.IsActiveMembershipAsync`.
- **Scheduling:** `ISchedulingApi` (`GetAppointmentForVisitAsync`, `StartVisitAsync` CheckedIn → InProgress, `CompleteVisitAsync` InProgress → Completed).
- **Clinical module** (`clinical` schema, 10 tables): `Visit` (rowversion, one per appointment), `ClinicalNote`, `VitalSign`, `Diagnosis` (ICD-10 format, one primary), `Prescription` + `PrescriptionItem`, `VisitAttachment`, `ClinicalAddendum`, `PatientAllergy`, `PatientCondition`.
- **`api/visits`:** open, search, view (audited), note, vitals, diagnoses, prescription, attachments (upload PDF/JPEG/PNG ≤ 10 MB with signature check; download audited), complete, addenda.
- **`api/patient-clinical`:** clinical summary (audited), allergies and conditions add/remove. **`api/portal/prescriptions`:** completed visits' prescriptions for linked patients.
- **Infrastructure:** `IFileStorage` + `LocalFileStorage` (`FileStorage:RootPath`, default `App_Data/files`, ignored by git).
- Migrations `Clinic/AddDoctorMembership` and `Clinical/InitialCreate`, applied to `Shifa`.

Verification (no automated tests, ADR-009): build clean; `has-pending-model-changes` = none. **End-to-end smoke run over HTTP, 46/46 checks passed**, against a separate database `Shifa_Smoke` (created for this; the dev database was not touched by the run). The run covered:
- linking the doctor to a staff account; booking and check-in;
- refusing a visit before check-in, and refusing an unlinked doctor (403);
- opening a visit, and that opening again returns the same visit; appointment InProgress → Completed;
- a nurse-role user recording vitals while another doctor can't write the note;
- refusing implausible vitals, an invalid ICD-10 code and two primary diagnoses;
- a PDF upload, and refusing a renamed binary; downloading the file;
- after completion, note, vitals and attachments refused, and an addendum accepted;
- allergies, conditions and the patient summary;
- portal prescriptions, and the portal refused on staff visit endpoints;
- an audit row for every clinical read and write.
The script is in the session scratchpad, not the repo.

Left open:
- Existing tenants' Doctor/Nurse roles don't get `Clinical.Vitals` automatically.
- No size quota per tenant for attachments.
- The `Shifa_Smoke` database can be dropped any time; ask before doing it.

## Phase 5 review (2026-11-07, opus / high)

Scope: booking transaction, availability calculator, lifecycle, idempotency, branch scope, portal rules. My own read plus an independent reviewer that didn't see my conclusions. Both found **no way to double-book a doctor**: every appointment claims 5-minute UTC ticks, and the unique index `(TenantId, DoctorId, SlotStart) WHERE IsDeleted = 0` covers every branch.

| # | Severity | Finding | Fix |
|---|---|---|---|
| S1 | Medium | A reschedule to the same start, racing a cancel, could leave a cancelled appointment holding live ticks: EF sent no UPDATE, so the rowversion was never checked | Reschedule always updates the appointment row (`IsModified`), so the rowversion is checked → 409 |
| S2 | Medium | The portal list returned the staff `AppointmentDto` (booking source, staff-written cancel reason, timestamps) | New `PortalAppointmentDto` |
| S3 | Low-Med | No-show kept its ticks while availability treated the time as free → offered slots failed with 409 | No-show releases the ticks in the same save |
| S4 | Low-Med | Idempotency keys were shared by every caller in a tenant: one caller's key could block another for 24 h, or replay an identical request | Keys scoped to the caller: `BookAppointment:m:{membershipId}` / `BookAppointment:p:{hash of phone}` |
| S5 | Low | Check-in was refused when the 2-hour window crossed midnight (visit at 00:30, arrival at 23:45) | Window = from start − 2 h until the end of the appointment's local day |
| S6 | Low | Repeated check-in returned no queue number; a double-click race returned 409 | Repeat returns the same number; the race loser reloads and returns it |
| S7 | Low | Extreme dates (e.g. `9999-12-31`) overflowed time-zone math → 500 | Validators bound dates to 2000–2100 (400) |
| S8 | Low | Across a mid-window DST gap, a visit could end after the shift (wall-clock vs elapsed time). Cairo isn't affected (midnight transitions) | Calculator also checks the end in UTC against the window end |
| S9 | Low | SQL deadlock victims and app-lock timeouts became 500 | Mapped to 409 `ConcurrentChange` (nothing saved, safe to retry) |
| S10 | Low | `Patient.NoShowCount` was never updated (Scheduling owns no-show counts), so the patient view always showed 0 | Removed from `PatientDto` |

Verification: build clean; the API starts in Development (DI validated on build), `/health/ready` 200, Swagger 200, anonymous portal call → 401 in the `Response` shape. **Not test-verified:** the tests are paused (ADR-009), so S1–S9 were checked by reading and building only. Re-check them when the tests come back.

Left open:
- The `patients.Patients.NoShowCount` column is still in the database (always 0). Dropping it needs a migration; ask before running it.
- A Pending online booking can't be marked no-show (Pending → NoShow isn't an allowed transition), so it keeps its time until it is confirmed or cancelled. This is a business decision.
- The portal list shows ids, not doctor/branch/service names (names need `IClinicApi` lookups).

## HRM layout restructure (2026-11-07)

- 16 projects → 3 (`Core`, `Infrastructure`, `Shifa`); 323 types moved to one file each by a Roslyn script; namespaces follow folders without a `Shifa.` prefix.
- All DI registrations in `Shifa/Program.cs` (like HRM); the per-module `XxxModule.cs` files are gone.
- AutoMapper 14.0.0 (MIT) with profiles per feature (Branch, Specialty, Doctor, Service, Patient, Appointment, Plan, Tenant). Repos use `_mapper.Map<Entity>(model)`, `_mapper.Map(model, entity)` and `ProjectTo<Dto>`. DTOs built from several tables (Role, Staff, Plan, Tenant lists, portal patients) stay as LINQ projections, because the entities have no navigation properties.
- Bug avoided: with all DbContexts in one assembly, `ApplyConfigurationsFromAssembly` loaded every module's entities into every context. Each context now applies only `Infrastructure.Configurations.<Module>`. Then `has-pending-model-changes` = no changes for all 5 contexts (database untouched).
- Verification before removing tests: build clean, all 93 integration tests passed on the new layout (twice, before and after removing unused usings).
- Tests removed at the developer's request (to be added back after the project is finished). The full suite is in git at commit `db02aa4` (`tests/`).
- `AvailableSlot` moved to `Core/DTO/Scheduling` (needed by the AutoMapper profile). Scheduling `Events` records removed (unused until the outbox).

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
