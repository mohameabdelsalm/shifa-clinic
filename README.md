# Shifa — Multi-tenant Clinic Management SaaS

One platform, many customer organizations (tenants). Each tenant manages its own branches, doctors, services and prices, staff and permissions, patients, appointments, medical records, invoices and reports, with strict data isolation.

Built as a **multi-tenant modular monolith** on ASP.NET Core .NET 10 and SQL Server, with module boundaries that allow extracting a module into a service later.

## Documentation

| Where | What |
|---|---|
| [project/progress.md](project/progress.md) | Current phase, what's done, test results, next tasks — **start here** |
| [project/implementation-roadmap.md](project/implementation-roadmap.md) | Phases 0–10 |
| [project/assumptions.md](project/assumptions.md) | Business defaults and items needing legal review |
| [docs/vision.md](docs/vision.md) | Product, users, scope, business rules |
| [docs/architecture/](docs/architecture/) | System, module boundaries, tenancy, database (ERD), security |
| [docs/decisions/](docs/decisions/) | ADR-001..007 |
| [CLAUDE.md](CLAUDE.md) | Rules and conventions for developers and Claude Code |

## Running locally

Prerequisites: .NET 10 SDK, SQL Server running locally as the default instance (`.`, Windows auth), Docker Desktop for Redis/Seq/Mailpit/Azurite (optional until later phases).

```bash
dotnet build Shifa.slnx
dotnet test --solution Shifa.slnx
dotnet run --project src/Shifa.API
```

The API listens on `http://localhost:5080` (Swagger at `/swagger`, health at `/health/live` and `/health/ready`).

Apply migrations to the dev database (one command per module):

```bash
dotnet ef database update -p src/Modules/Tenancy/Shifa.Tenancy -s src/Shifa.API -c TenancyDbContext
dotnet ef database update -p src/Modules/Identity/Shifa.Identity -s src/Shifa.API -c IdentityDbContext
dotnet ef database update -p src/Modules/Clinic/Shifa.Clinic -s src/Shifa.API -c ClinicDbContext
```

Create the first Platform Admin (an explicit command; the password comes from user-secrets, never from a file in the repo):

```bash
dotnet user-secrets --project src/Shifa.API set "PlatformAdmin:Email" "you@example.com"
dotnet user-secrets --project src/Shifa.API set "PlatformAdmin:Password" "<a strong password>"
dotnet run --project src/Shifa.API -- --seed-platform-admin
```

Then sign in with `POST /api/auth/login` (no `tenantSlug`) and use `api/platform/plans` and `api/platform/tenants` to onboard a customer.

Integration tests create and migrate `Shifa_Tests` on the local instance automatically (override with `SHIFA_TEST_CONNECTION`).

## Solution layout

```
src/
├─ Shifa.API/              host: Program.cs, Middleware, Extensions
├─ Shifa.Core/             Response, BusinessException, BaseModel, TenantOwnedEntity, interfaces, resources
├─ Shifa.Infrastructure/   tenant context, current user, ModuleDbContext, interceptors, validation
└─ Modules/
   ├─ Tenancy/  Shifa.Tenancy + Shifa.Tenancy.Contracts
   ├─ Identity/ Shifa.Identity + Shifa.Identity.Contracts
   └─ Clinic/   Shifa.Clinic  + Shifa.Clinic.Contracts
tests/
├─ Shifa.ArchitectureTests/   module boundaries, entity ownership, tenant keys
└─ Shifa.IntegrationTests/    real SQL Server: tenant isolation, API foundation
```
