# ADR-006 — Local-first development

- **Status:** Accepted
- **Date:** 2026-11-07

## Context

Hosting (cloud provider, source control host) is not chosen yet and must not block development or leak into code.

## Decision

| Concern | Local | Production (to be decided) |
|---|---|---|
| Database | SQL Server 2022 Developer, default instance `.`, Windows auth | Licensed SQL Server or Azure SQL |
| Integration tests | Database `Shifa_Tests` on the same instance, reset with Respawn | Testcontainers in CI |
| Cache | Redis (Docker) | Managed Redis |
| Logs | Serilog → console + Seq (Docker, `localhost:5341`) | Central log store |
| Email | Mailpit (Docker, `localhost:8025`), or the in-memory fake (`/dev/email`); sent with MailKit (ADR-015) | SMTP provider |
| SMS / WhatsApp | Fake sender (log + notifications table) | Real provider |
| Files | Azurite (Docker) — private container | Private object storage |
| Source control | Local git | GitHub or Azure DevOps + CI |

External services are behind interfaces; switching is configuration only. Until CI exists, run `dotnet build` and `dotnet test` before every commit.

## Consequences

- No cloud cost or accounts during development.
- Production configuration, secrets, backups and monitoring are designed in Phase 10.
