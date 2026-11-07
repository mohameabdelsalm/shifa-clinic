# Shifa — Multi-tenant Clinic Management SaaS

Multi-tenant modular monolith (ASP.NET Core .NET 10, SQL Server, EF Core 10).

## Start of every session

1. Read `project/progress.md` (current phase, what's done, next tasks).
2. Read only the docs relevant to the task: `docs/architecture/*.md`, `docs/decisions/ADR-*.md`, `project/assumptions.md`.
3. Don't re-read the whole repository; search for what you need.

## Working with the developer

- Explain in Arabic with English technical terms inline: business problem first, then the design, then the code flow.
- Keep the Trio ERP HRM naming and folder conventions (ADR-004). Don't introduce new names for things HRM already names.
- At the start of each phase, give the execution header (phase, model alias, effort, reason, complexity). The developer switches models from the app's model picker.
- Phase done = build green, tests green, docs + `project/progress.md` updated, merged to `develop`.

## Solution layout

```
src/
├─ Shifa.API/              Program.cs, Middleware/, Extensions/  (host only)
├─ Shifa.Core/             Common/, Entities/ (BaseModel, TenantOwnedEntity), Enums/, Interfaces/, LanguageResource
├─ Shifa.Infrastructure/   Common/ (CurrentUserManager, TenantManager, Validation), Data/, Caching/, Services/
└─ Modules/<Module>/
   ├─ Shifa.<Module>/            implementation (HRM folders)
   └─ Shifa.<Module>.Contracts/  I<Module>Api, Events/
tests/
├─ Shifa.ArchitectureTests/
└─ Shifa.IntegrationTests/        real SQL Server (Shifa_Tests)
```

## Multi-tenancy rules (ADR-002, docs/architecture/tenancy-design.md) — security critical

- Tenant ID comes only from the validated token via `ITenantContext`. Never accept `TenantId` from a request body, query string or header. Models (`Add<Name>Model` etc.) have no `TenantId`.
- Tenant-owned entities inherit `TenantOwnedEntity`; global entities are `[PlatformOwned]`. Every entity is one or the other.
- FKs between tenant-owned entities are composite `(TenantId, XId)` using `HasPrincipalKey(p => new { p.TenantId, p.Id })`.
- Business-unique values are unique per tenant: index `(TenantId, Code)`.
- Never use `IgnoreQueryFilters` except in platform repos, with the filter name and an audit record.
- Background jobs and event consumers receive `TenantId` explicitly and set the context before touching data.
- Cache keys start with `t:{tenantId}:`; file paths start with `tenants/{tenantId}/`.
- Every read by ID also checks branch scope and, for patient data, patient access. Out-of-scope → 404.

## Authorization rules (ADR-007)

- Endpoints require `[Permission(Permissions.X)]` (tenant data) or `[PlatformAdmin]` (`api/platform/...`). Anonymous endpoints say `[AllowAnonymous]` explicitly.
- Permissions live in `Shifa.Core.Common.Permissions`. Adding a feature = add its codes there (and to the default roles).
- Rate-limit new anonymous or SMS/OTP-sending endpoints with `[EnableRateLimiting("auth")]`.
- A repo that hands out roles, permissions or branches must check the caller holds them (`IAccessContext`) — see `StaffRepo`/`RoleRepo`.
- Write an audit row (`IAuditService`) for security-relevant changes and for every Platform Admin action.
- Another tenant's id returns 404, never 403.
- New tenant-owned endpoints get a cross-tenant test in `Shifa.IntegrationTests/Api` (read, update, delete by Tenant B must be 404).

## Architecture rules (ADR-001, docs/architecture/module-boundaries.md)

- `Shifa.Core` → nothing. `Shifa.Infrastructure` → Core. `Shifa.<Module>.Contracts` → Core only.
- `Shifa.<Module>` → Core, Infrastructure, own Contracts, other modules' Contracts. Never another module's implementation.
- Only `Shifa.API` and tests reference module implementations.
- One `DbContext` (inherits `ModuleDbContext`), schema and migrations per module. No FKs or joins across schemas.
- Cross-module: `I<Module>Api` for queries; integration events through the outbox for side effects (ADR-005).
- `Shifa.ArchitectureTests` enforces this. Never weaken or delete a test; fix the code.

## Inside a module (ADR-004)

- Folders: `Controllers/`, `Entities/`, `Enums/`, `DTO/`, `Models/`, `Interfaces/`, `Repositories/`, `Validators/`, `Profiles/`, `Configurations/`, `Data/`, `Migrations/`, `Resources/`, `<Module>Module.cs`.
- Names: `Add<Name>Model`, `Update<Name>Model`, `Search<Name>sModel` (input); `<Name>Dto`, `Search<Name>sDto` (output); `I<Name>Repo`/`<Name>Repo`; `<Name>Controller`; `<Name>Configuration`; `<Name>Profile`; `Add<Name>Validator`; enums `<Name>Enum`.
- Flow: `Controller → I<Name>Repo → <Name>Repo → Response`. Controllers stay thin.
- Repo reads: `AsNoTracking` projections to DTOs. Repo writes: load entity → call entity method (rules live on entities) → `SaveChangesAsync`.
- Manual mapping in `Profiles/`. No AutoMapper, MediatR or generic repository. Split repos over ~400 lines.

## Responses, errors, validation

- Every endpoint returns `Shifa.Core.Common.Response` (HRM shape). HTTP status = `Response.Status`.
- Messages come from `LanguageResource` (en, ar) or module resources — never hard-coded.
- Business errors: `throw new BusinessException(message, status)`. Unexpected errors: 500 with a generic message; details in logs only.
- FluentValidation validators run automatically for action arguments; the first error is returned as a 400 `Response`.

## Conventions

- Package versions only in `Directory.Packages.props`; check licenses before adding packages.
- Routes `api/<resource>`; platform endpoints `api/platform/<resource>`.
- IDs `long` via HiLo (ADR-003). Money `decimal(18,2)`. Time: inject `TimeProvider`; store UTC `datetimeoffset`.
- `rowversion` on concurrently edited entities. Soft delete via `IsDeleted`; medical and financial records are never hard-deleted.
- Use `CancellationToken` on I/O. Async EF calls.
- Never log medical data, national IDs, passwords, OTPs or tokens.
- No destructive database operations (drop/recreate) without explicit approval. Migrations are applied explicitly, never at app startup.

## Patient portal rules (ADR-007)

- Portal endpoints (`api/portal/...`) use `[PortalUser]`; the phone number comes from `IAccessContext.PortalPhoneNumber` (the token), never from the request.
- Anything a portal user can reach must be filtered by `PatientAccountAccess` for that phone; another clinic's or unlinked patient is 404.
- Portal DTOs are separate from staff DTOs and never carry identifiers, national IDs or internal counters.
- OTP-style endpoints never reveal whether a phone number or clinic exists (same answer, same 401), and are rate limited. Never log codes or phone numbers.

## Local environment (ADR-006)

- SQL Server `.` (Windows auth): `Server=.;Database=Shifa;Trusted_Connection=True;TrustServerCertificate=True`. Tests: `Shifa_Tests`.
- `GET /dev/sms` (Development only) shows the SMS the fake sender "sent", e.g. patient OTP codes.
- `docker compose up -d`: Redis, Seq (`localhost:5341`), Mailpit (`localhost:8025`), Azurite.
- `dotnet build Shifa.slnx` and `dotnet test --solution Shifa.slnx` before every commit.

## Git

- `main` = releases, `develop` = integration, `feature/<name>` from `develop`. Conventional Commits.
