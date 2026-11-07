# Progress

_Last updated: 2026-11-07_

## Phases

| Phase | State |
|---|---|
| 0 — Architecture finalization | ✅ Done |
| 1 — Solution foundation | ✅ Done |
| 2 — Identity, Tenancy, Platform Admin | ⏳ Next |
| 3–10 | Not started (see `implementation-roadmap.md`) |

## Current phase

**Phase 2 — Identity, Tenancy & Platform Administration** (security critical). Model: sonnet / medium for implementation, opus / high for the isolation and security review.

## Done in Phase 0

- ADR-001..007 in `docs/decisions/` (multi-tenant modular monolith, shared DB + TenantId, SQL Server + HiLo `long` keys, HRM code conventions — chosen over the prompt's vertical slices by the product owner, outbox, local-first, authN/authZ model).
- Architecture docs in `docs/architecture/`: system, module boundaries, tenancy, database (revised ERD with TenantId and composite keys), security.
- `project/assumptions.md` (business defaults, legal review items), `project/implementation-roadmap.md`.

## Done in Phase 1

- Shared (HRM names): `Response`, `BusinessException`, `BaseModel`, `TenantOwnedEntity`, `[PlatformOwned]`, `StatusEnum`, `ICurrentUser`, `ITenantContext`, `LanguageResource` (en, ar), `ShifaClaimTypes`.
- Tenancy foundation: scoped set-once `TenantContext`; `TenantResolutionHandler` (token claim → context); `ModuleDbContext` with schema, HiLo, named filters `Tenant` + `SoftDelete`, `(TenantId, Id)` alternate key, decimal/enum conventions; `AuditTenantInterceptor` (stamp TenantId, block cross-tenant writes, audit columns, soft delete).
- API: Serilog (console; Seq in Development), localization en/ar, `GlobalExceptionHandler` (`IExceptionHandler`), model-state + FluentValidation `ValidationFilter` returning `Response`, Swagger (Development only), HSTS/HTTPS outside Development, `/health/live`, `/health/ready` (DbContext checks).
- Modules: Tenancy (`Tenant`, platform-owned) and Clinic (`Branch`, tenant-owned), each with its own DbContext, schema and `InitialCreate` migration.
- `docker-compose.yml` (Redis, Seq, Mailpit, Azurite). Docker Desktop not running yet; nothing in Phase 1 needs it.
- Security pin: `System.Security.Cryptography.Pkcs` 10.0.14 (transitive from Microsoft.Data.SqlClient 9.0.11 had known vulnerabilities).

## Database migrations

| Module | Migration | Applied to |
|---|---|---|
| Tenancy | `InitialCreate` (schema `tenancy`, `Tenants`, HiLo sequence) | `Shifa` (dev), `Shifa_Tests` |
| Clinic | `InitialCreate` (schema `clinic`, `Branches`, HiLo sequence) | `Shifa` (dev), `Shifa_Tests` |

## Tests

`dotnet test --solution Shifa.slnx` → **29 passed, 0 failed** (2026-11-07).

- Architecture (12): project references, every module has Contracts, every entity tenant- or platform-owned, Tenant filter + `(TenantId, Id)` key on tenant entities, composite tenant FKs, one schema per module.
- Integration on SQL Server (17): tenant sees only its rows, other tenant's row not found by id, no context → no rows, TenantId stamped, insert/update for another tenant rejected, TenantId change rejected (EF key protection), save without context rejected, unique code per tenant, HiLo id before save, soft delete; platform entity save, unique slug; health live/ready; exception handler 500 (en/ar, no leak) and BusinessException mapping.

## Known issues / notes

- Tenant resolution trusts the token claim only; membership and tenant status validation arrive with Identity in Phase 2. No endpoints exist yet, so nothing is exposed.
- Swagger JWT security definition is added in Phase 2 with authentication.
- EF tools 10.0.7 are older than runtime 10.0.14 (warning only): `dotnet tool update -g dotnet-ef`.
- OpenTelemetry is deferred to Phase 10 (roadmap).

## Next tasks (Phase 2)

1. Identity module: `User` (ASP.NET Core Identity, `long` keys), `Membership`, roles, permission catalog, `MembershipBranch`, refresh tokens, OTP.
2. Tenancy module: tenant onboarding (default roles, platform admin flow), plans, entitlements, subscriptions.
3. JWT + refresh rotation; tenant-scoped tokens; switch-tenant endpoint.
4. `TenantResolutionHandler`: validate membership and tenant status (cached) before setting the context.
5. `[Permission]` policies, branch scope, Platform Admin policy + audit log.
6. Integration tests: Tenant A cannot read, update or delete Tenant B data through the API.
