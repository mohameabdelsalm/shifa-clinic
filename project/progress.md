# Progress

_Last updated: 2026-11-07_

## Phases

| Phase | State |
|---|---|
| 0 — Architecture finalization | ✅ Done |
| 1 — Solution foundation | ✅ Done |
| 2 — Identity, Tenancy, Platform Admin | ✅ Implemented; **opus / high security review still to do** |
| 3 — Clinic setup | ⏳ Next |
| 4–10 | Not started (see `implementation-roadmap.md`) |

## Current phase

**Phase 3 — Clinic setup** (branches, doctors, specialties, services, prices, schedules, time off). Model: sonnet / medium. Before it, run the Phase 2 review with opus / high (see "Review checklist" below).

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
| Tenancy | `InitialCreate`, `AddPlansAndSubscriptions` | `Shifa` (dev), `Shifa_Tests` |
| Clinic | `InitialCreate` | `Shifa` (dev), `Shifa_Tests` |
| Identity | `InitialCreate` (users, memberships with composite tenant FKs, roles, permissions, tokens, audit) | `Shifa` (dev), `Shifa_Tests` |

## Tests

`dotnet test --solution Shifa.slnx` → **56 passed, 0 failed** (2026-11-07). Mutation check: with the `Tenant` query filter replaced by `true`, 8 isolation tests fail (then restored).

- Architecture (12) and Phase 1 integration (17), unchanged.
- API integration (27), all through HTTP against SQL Server:
  - Authentication: tokens, identical 401s for 5 failure kinds, lockout, refresh rotation + family revocation, logout, one person in two tenants + switch, bad tokens, platform token has no tenant permissions.
  - **Tenant isolation:** each tenant sees only its roles; B can't read/update/delete A's roles; B can't read/update/suspend A's staff; A can't assign B's roles; membership lookup never crosses tenants; suspended tenant locked out immediately (and a stale token doesn't block signing in elsewhere).
  - Authorization: 403 without permission, staff-manager and role-manager escalation blocked, system roles protected, suspended member loses access at once, can't suspend self, plan employee limit, unknown branch ids, platform endpoints reject anonymous and tenant admins, onboarding validation, audit rows for platform actions, tenant list.

## Review checklist for the Phase 2 opus / high review

- Walk every `[AllowAnonymous]` endpoint and every repo method that reads by id (IDOR).
- `TenantResolutionHandler`: order in the pipeline, behaviour on cache staleness across instances (30 s), JWT key handling in production.
- `AuditTenantInterceptor` + composite FKs under bulk operations (`ExecuteUpdate/Delete`) — none exist yet.
- Onboarding compensation (`TenantRepo.Add`): tenant and identity data are in different schemas without a shared transaction.
- Refresh/login rate limits are per IP only (behind a proxy the forwarded IP must be configured).

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
