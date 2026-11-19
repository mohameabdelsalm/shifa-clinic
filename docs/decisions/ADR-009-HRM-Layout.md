# ADR-009 — Same project layout as Trio HRM/Auth

- **Status:** Accepted (supersedes the project split of ADR-001 and the module-project layout of ADR-004)
- **Date:** 2026-11-07
- **Decided by:** product owner, 2026-11-07

## Context

Phases 0–5 used a modular-monolith project split: `src/Shifa.Core`, `src/Shifa.Infrastructure`, `src/Shifa.API`, and two projects per module (`Shifa.<Module>` + `Shifa.<Module>.Contracts`) — 16 projects with test projects. Inside each module the HRM folder names were used, but the overall shape was different from the team's Trio ERP HRM and Auth APIs, which have three projects at the repository root (`Core`, `Infrastructure`, the API). The product owner wants Shifa to look and feel exactly like HRM so the team can work in it without learning a new structure.

## Decision

- Three projects at the repository root, no `src/` or `tests/` folders: `Core/`, `Infrastructure/`, `Shifa/` (the API, like `HRM/`).
- Folders inside them follow HRM, split by module (Clinic, Identity, Patients, Scheduling, Tenancy):
  - `Core`: `Entities/<M>`, `Enums/<M>`, `DTO/<M>`, `Models/<M>`, `Interfaces/<M>`, `Profiles/<M>`, `Validations/<M>`, plus `Common`, `Pagination`, `Resources`.
  - `Infrastructure`: `Data` (all DbContexts), `Configurations/<M>`, `Migrations/<M>`, `Repositories/<M>`, `Services/<M>`, `Auth`, `Common`.
  - `Shifa`: `Controllers/<M>`, `Middleware`, `Extensions`, `Program.cs` (all DI registrations, like HRM).
- One class per file. Namespaces follow folders (`Core.Entities.Scheduling`, `Infrastructure.Repositories.Patients`, `Shifa.Controllers.Clinic`).
- AutoMapper (14.0.0, the last MIT version) with `Profile` classes, used like HRM: model → entity on add/update, entity → DTO on reads (`ProjectTo`). Advisory GHSA-rvv3-g6hj-g44x (deep recursion on self-referencing graphs) is suppressed by name in `Directory.Build.props`; Shifa maps only flat models/DTOs.
- Kept from earlier ADRs: one DbContext, schema and migrations history per module (ADR-002/003); modules talk through `I<Module>Api` interfaces (now in `Core/Interfaces/<M>`); all tenancy, authorization and booking rules.
- Tests are paused at the developer's request until the project is finished; the last suite (93 integration + 30 unit + 12 architecture tests) is in the git history before this change and passed on the new layout before it was removed.

## Consequences

- The compiler no longer stops one module from using another module's DbContext or repositories. This is now a review rule (CLAUDE.md "Data and module rules"). Extracting a module into a service later needs more work than before.
- Because all DbContexts live in one assembly, each context applies only the configurations in `Infrastructure.Configurations.<M>` (`ModuleDbContext.Module`). Without that, every context would pick up every module's entities.
- Moving the classes changed only CLR names; `has-pending-model-changes` reports no changes for all five contexts, so the database is untouched and the migration history is unchanged.
- The `Contracts` events (Scheduling) were removed as unused; they come back with the outbox (ADR-005).
