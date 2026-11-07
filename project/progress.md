# Progress

_Last updated: 2026-11-07_

## Phases

| Phase | State |
|---|---|
| 0 — Architecture finalization | ✅ Done |
| 1 — Solution foundation | ✅ Done |
| 2 — Identity, Tenancy, Platform Admin | ✅ Done, security-reviewed (opus / high) and fixed |
| 3 — Clinic setup | ⏳ Next |
| 4–10 | Not started (see `implementation-roadmap.md`) |

## Current phase

**Phase 3 — Clinic setup** (branches, doctors, specialties, services, prices, schedules, time off). Model: sonnet / medium.

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
- Staff listing isn't branch-scoped yet (Phase 3 with branch endpoints). No CORS or security headers yet (Phase 10).

## Done in Phase 0–1

See git history. Highlights: ADR-001..007, architecture docs, `ModuleDbContext` (schema, HiLo, `Tenant`/`SoftDelete` filters, `(TenantId, Id)` keys), `AuditTenantInterceptor`, `GlobalExceptionHandler`, validation filter, health checks, Serilog, Tenancy + Clinic initial migrations.

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
| Clinic | `InitialCreate` | `Shifa` (dev), `Shifa_Tests` |
| Identity | `InitialCreate` (users, memberships with composite tenant FKs, roles, permissions, tokens, audit) | `Shifa` (dev), `Shifa_Tests` |

## Tests

`dotnet test --solution Shifa.slnx` → **63 passed, 0 failed** (2026-11-07, after the security review). Mutation check: with the `Tenant` query filter replaced by `true`, 8 isolation tests fail (then restored).

- Architecture (12) and Phase 1 integration (17), unchanged.
- API integration (27), all through HTTP against SQL Server:
  - Authentication: tokens, identical 401s for 5 failure kinds, lockout, refresh rotation + family revocation, logout, one person in two tenants + switch, bad tokens, platform token has no tenant permissions.
  - **Tenant isolation:** each tenant sees only its roles; B can't read/update/delete A's roles; B can't read/update/suspend A's staff; A can't assign B's roles; membership lookup never crosses tenants; suspended tenant locked out immediately (and a stale token doesn't block signing in elsewhere).
  - Authorization: 403 without permission, staff-manager and role-manager escalation blocked, system roles protected, suspended member loses access at once, can't suspend self, plan employee limit, unknown branch ids, platform endpoints reject anonymous and tenant admins, onboarding validation, audit rows for platform actions, tenant list.

## Known gaps / notes

- Patient OTP login, password reset/change, subscription-expiry enforcement (assumption A6), JWT key rotation and Platform Admin usage monitoring are deferred (ADR-007).
- No branch endpoints yet, so branch-scope assignment is only tested for rejection of unknown ids; positive branch-scope tests come with Phase 3.
- `AuditService` saves through the request's `IdentityDbContext`; an audit failure after a business change surfaces as an error to the caller (change already committed).
- EF tools 10.0.7 older than runtime 10.0.14 (warning): `dotnet tool update -g dotnet-ef`.
- OpenTelemetry deferred to Phase 10. Docker not needed yet.

## Next tasks (Phase 3)

1. Clinic: `Specialty`, `Doctor`, `DoctorSpecialty`, `Service`, `ServicePrice`, `DoctorBranch`, `DoctorSchedule`, `DoctorTimeOff`; branch CRUD with `MaxBranches` / `MaxDoctors` plan limits.
2. Branch-scope authorization on branch-owned data; composite tenant FKs for all of it.
3. Tests: tenant isolation + branch scope for every new endpoint (read/update/delete by another tenant → 404).
