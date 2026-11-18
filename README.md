# Shifa — Multi-tenant Clinic Management SaaS

One platform, many customer organizations (tenants). Each tenant manages its own branches, doctors, services and prices, staff and permissions, patients, appointments, medical records, invoices and reports, with strict data isolation.

Built as a **multi-tenant monolith** on ASP.NET Core .NET 10 and SQL Server, organized like the Trio ERP HRM API (Core / Infrastructure / API, one class per file, AutoMapper). Each module keeps its own database schema.

## Documentation

| Where | What |
|---|---|
| [project/progress.md](project/progress.md) | Current phase, what's done, next tasks — **start here** |
| [project/implementation-roadmap.md](project/implementation-roadmap.md) | Phases 0–10 |
| [project/assumptions.md](project/assumptions.md) | Business defaults and items needing legal review |
| [docs/vision.md](docs/vision.md) | Product, users, scope, business rules |
| [docs/architecture/](docs/architecture/) | System, module boundaries, tenancy, database (ERD), security |
| [docs/decisions/](docs/decisions/) | ADR-001..015 |
| [CLAUDE.md](CLAUDE.md) | Rules and conventions for developers and Claude Code |

## Running locally

Prerequisites: .NET 10 SDK, SQL Server running locally as the default instance (`.`, Windows auth), Docker Desktop for Redis/Seq/Mailpit/Azurite (optional until later phases).

```bash
dotnet build Shifa.slnx
dotnet run --project Shifa
```

The API listens on `http://localhost:5080` (Swagger at `/swagger`, health at `/health/live` and `/health/ready`).

Apply migrations to the dev database (one command per module):

```bash
dotnet ef database update -p Infrastructure -s Shifa -c TenancyDbContext
dotnet ef database update -p Infrastructure -s Shifa -c IdentityDbContext
dotnet ef database update -p Infrastructure -s Shifa -c ClinicDbContext
dotnet ef database update -p Infrastructure -s Shifa -c PatientsDbContext
dotnet ef database update -p Infrastructure -s Shifa -c SchedulingDbContext
dotnet ef database update -p Infrastructure -s Shifa -c ClinicalDbContext
```

Create the first Platform Admin (an explicit command; the password comes from user-secrets, never from a file in the repo):

```bash
dotnet user-secrets --project Shifa set "PlatformAdmin:Email" "you@example.com"
dotnet user-secrets --project Shifa set "PlatformAdmin:Password" "<a strong password>"
dotnet run --project Shifa -- --seed-platform-admin
```

Patient portal sign-in is by phone OTP (`POST /api/portal/auth/otp/request` then `.../verify`); locally nothing is really sent, read the code at `GET /dev/sms` (Development only).

Then sign in with `POST /api/auth/login` (no `tenantSlug`) and use `api/platform/plans` and `api/platform/tenants` to onboard a customer.

## Solution layout (same as Trio HRM)

```
Shifa.slnx
Core/             Entities, DTO, Models, Enums, Interfaces, Profiles (AutoMapper), Validations — each split by module
Infrastructure/   Data (DbContexts), Configurations, Migrations, Repositories, Services, Auth — each split by module
Shifa/            the API: Program.cs, Controllers, Middleware, Extensions
```

Modules: Tenancy, Identity, Clinic, Patients, Scheduling, Clinical. Tests are paused for now (see `project/progress.md`).
