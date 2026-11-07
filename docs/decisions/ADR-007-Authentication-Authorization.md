# ADR-007 — Authentication and authorization model

- **Status:** Accepted
- **Date:** 2026-11-07

## Context

Users include platform staff (the product owner), tenant staff with different roles and branches, and patients or guardians using a portal. One person may belong to more than one tenant. A wrong decision here leaks data across tenants, branches or patients.

## Decision

- **Authentication:** ASP.NET Core Identity for users and password hashing; short-lived JWT access tokens (15 min) and rotating refresh tokens stored hashed. Patients may sign in with phone OTP. Users are global (one account per person); access to a tenant comes from a membership.
- **Tenant-scoped tokens:** an access token is issued for one tenant (`tenant_id`, `membership_id` claims). Switching tenant issues a new token. On every request the server validates that the membership and the tenant are active (cached briefly) before setting `ITenantContext`.
- **RBAC:** permissions are a code-defined catalog (`Appointments.Create`, `Clinical.Read`, ...). Roles are per tenant and group permissions; default roles (TenantAdmin, BranchManager, Doctor, Receptionist, Cashier, Nurse) are created when a tenant is onboarded. Endpoints require permissions via `[Permission("...")]`, as in the Trio Auth API.
- **Branch scope:** a membership has either all-branches access or an explicit branch list. Branch-scoped operations check it on the server for every request.
- **Patient scope:** a portal user sees only patient profiles linked to them by `PatientAccountAccess` (self, child, ward) inside the current tenant.
- **Platform scope:** Platform Admin is a separate claim and policy, with endpoints under `api/platform/...`. Cross-tenant data access is explicit (`IgnoreQueryFilters` on a named filter) and always audited.

## Implementation notes (Phase 2)

- **Passwords:** `PasswordHasher<User>` from ASP.NET Core Identity (PBKDF2, rehash on sign-in). The full Identity stores/`IdentityDbContext` are not used: `User` is a plain `BaseModel` entity, so every module keeps the same `ModuleDbContext` rules. Lockout: 5 failures → 15 minutes.
- **Same generic 401** for wrong password, unknown user, unknown or inactive tenant, missing membership and locked accounts; unknown users still run a password verification (timing).
- **Refresh tokens:** random, stored as SHA-256, rotated on every use, grouped in a family; reuse of a used token revokes the family and is audited. Refresh re-checks user, tenant and membership.
- **Per-request validation** (`TenantResolutionHandler`): tenant active (`ITenancyApi`) and membership active for the token's user (`IIdentityApi`), both cached 30 s; role/membership/tenant changes invalidate the cache in-process (version per tenant), so a second instance sees a change within 30 s. `AllowAnonymous` endpoints skip this check so a stale token can't block sign-in.
- **Tenant scopes:** lookups for a tenant other than the request's (login, membership, onboarding) run in `ITenantScopeFactory`, a new DI scope with its own `ITenantContext`. No `IgnoreQueryFilters` outside platform code.
- **Escalation guards:** you can only create/edit roles with permissions you hold, assign roles whose permissions you hold, and assign branches inside your own scope. The `TenantAdmin` role can't be edited; system roles can't be deleted; you can't suspend yourself.
- **Platform Admin:** `platform_admin` claim + per-request re-check; has no tenant permissions (`403` on tenant endpoints). First admin is created by an explicit command: `dotnet run --project src/Shifa.API -- --seed-platform-admin` with `PlatformAdmin:Email/Password/FullName` from user-secrets.
- **Rate limiting:** `auth` policy (10/min per IP, `RateLimiting:AuthPermitLimit`) on `api/auth`.
- **Deferred:** patient OTP login (Phase 4 with the patient portal), password reset/change, subscription-expiry enforcement (assumption A6), JWT key rotation, usage monitoring for the Platform Admin.

## Consequences

- One login per person, with explicit tenant selection; a token can never act on two tenants.
- Membership and tenant status are checked per request, so suspending a tenant takes effect within the cache TTL.
- Every repo method that reads by ID must check tenant (query filter + key), branch and, for patient data, patient access.

## Alternatives considered

- **Tenant ID in a request header:** the client controls it. Rejected (only allowed as a hint to pick which membership to issue a token for, at login).
- **External identity provider now (Keycloak, Entra ID):** more infrastructure than needed for v1; can be adopted later behind the same claims.
