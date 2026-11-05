# Shifa — Clinic Management Backend

Backend for a multi-branch outpatient clinic group: online booking, reception queue, electronic medical records (EMR), billing and automated reminders.

Built as a **modular monolith** on ASP.NET Core (.NET 10) with strict module boundaries, so modules can be extracted into microservices later without a rewrite.

## Documentation

| Document | What it covers |
|---|---|
| [docs/vision.md](docs/vision.md) | The client, the problems we solve, users, MVP scope, business rules |
| [docs/erd.md](docs/erd.md) | Data model per module, constraints and indexes |
| [docs/adr/](docs/adr/) | Architecture decisions and why we made them |
| [CLAUDE.md](CLAUDE.md) | Project rules and conventions (for developers and Claude Code) |

## Stack

- .NET 10 / ASP.NET Core Web API (controllers, same conventions as the Trio ERP HRM API)
- SQL Server 2022 (one schema per module) + EF Core 10
- Redis (HybridCache L2, OTP, rate limiting)
- Hangfire (background jobs, SQL Server storage)
- Serilog + Seq, OpenTelemetry, Sentry

## Running locally

Prerequisites:

- .NET 10 SDK
- SQL Server running locally as the default instance (`.`) with Windows Authentication
- Docker Desktop (for Redis, Seq, Mailpit, Azurite)

```bash
dotnet build Shifa.slnx
dotnet test --solution Shifa.slnx
dotnet run --project src/Shifa.API      # http://localhost:5080
```

> `docker-compose.yml` is added in step 4.

## Solution layout

```
src/
├─ Shifa.API/                   host: Program.cs, Middleware, Extensions
├─ Shifa.Core/                  shared: Response, BusinessException, PagedList, BaseModel, interfaces
├─ Shifa.Infrastructure/        shared: caching, current user, outbox, jobs, files, email, SMS
└─ Modules/
   └─ Clinic/
      ├─ Shifa.Clinic/          implementation (HRM folder conventions)
      └─ Shifa.Clinic.Contracts/  events + IClinicApi: the only part other modules may use
tests/
└─ Shifa.ArchitectureTests/     fails the build when module boundaries are broken
```

## Status

| Step | Content | State |
|---|---|---|
| 1 | Repository, vision, ERD, ADRs, CLAUDE.md | ✅ Done |
| 2 | Solution skeleton | ✅ Done |
| 3 | Building blocks | ⏳ Next |
| 4 | Local infrastructure + first migration | ⏳ |
