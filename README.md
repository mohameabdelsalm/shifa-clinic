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

- .NET 10 / ASP.NET Core Minimal APIs
- SQL Server 2022 (one schema per module) + EF Core 10
- Redis (HybridCache L2, OTP, rate limiting)
- Hangfire (background jobs, SQL Server storage)
- Serilog + Seq, OpenTelemetry, Sentry

## Running locally

Prerequisites:

- .NET 10 SDK
- SQL Server running locally as the default instance (`.`) with Windows Authentication
- Docker Desktop (for Redis, Seq, Mailpit, Azurite)

> The solution and `docker-compose.yml` are added in the next steps. This section will be completed then.

## Status

| Step | Content | State |
|---|---|---|
| 1 | Repository, vision, ERD, ADRs, CLAUDE.md | ✅ Done |
| 2 | Solution skeleton | ⏳ Next |
| 3 | Building blocks | ⏳ |
| 4 | Local infrastructure + first migration | ⏳ |
