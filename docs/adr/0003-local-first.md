# ADR 0003 — Local-first development, hosting decided later

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

We haven't chosen where the code and the system will live (GitHub or Azure DevOps; Azure, another cloud or a VPS). We don't want that decision to block development or leak into the code.

## Decision

Everything runs on the developer's machine:

| Concern | Local | Later |
|---|---|---|
| Database | SQL Server default instance `.` | Azure SQL or SQL Server on the server |
| Cache | Redis in Docker | Managed Redis (or Valkey) |
| Logs | Seq in Docker (`localhost:5341`) | Sentry + Seq or Application Insights |
| Email | Mailpit in Docker (`localhost:8025`) | Real SMTP provider |
| SMS / WhatsApp | `FakeSmsSender` writes to the log and the `notifications` table | Real SMS provider |
| Files | Azurite in Docker | Azure Blob Storage |
| Source control | Local git repository | GitHub or Azure DevOps + CI |

Every external service is behind an interface (`ISmsSender`, `IEmailSender`, `IFileStorage`). Switching from local to real is configuration only.

Until there is CI, run `dotnet build` and `dotnet test` before every commit.

## Consequences

- Zero cloud cost and no accounts needed during development.
- The production setup must be designed and tested before release (week 10 in the plan).
- Integration tests run against the local SQL Server (`Shifa_Tests` database, reset with Respawn). They move to Testcontainers when CI is added.

## Alternatives considered

- **Pick a cloud on day one:** setup time and cost before anything is worth deploying. Rejected for now.
