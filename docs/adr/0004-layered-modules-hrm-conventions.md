# ADR 0004 — Layered modules using the team's HRM conventions

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

The team builds the Trio ERP APIs (Auth, HRM) with the same layout: `Core` / `Infrastructure` / API project, and every request flows `Controller → IXxxRepo → XxxRepo → Response`. Developers should feel at home in Shifa, so we keep those names and that flow.

What we saw in HRM that we must avoid here:

- Any repository can read or write any table, so no part can be extracted into a service later.
- `Program.cs` registers every repository and AutoMapper profile by hand (~250 lines).
- Repositories grow without limit (`EmployeeRepository` is 830 lines) because validation, mapping, business rules, data access and messaging all live there.
- RabbitMQ messages are published after `SaveChangesAsync`, so a crash in between loses them.
- No tests.

## Decision

### Module layout

Each module keeps the HRM folder names, inside its own project:

```
Shifa.<Module>/
├─ Controllers/        <Name>Controller           thin: calls the repo, returns Response
├─ Entities/           <Name>                     entities with methods that guard business rules
├─ Enums/
├─ DTO/                <Name>Dto, Search<Name>sDto        output
├─ Models/             Add<Name>Model, Update<Name>Model, Search<Name>sModel   input
├─ Interfaces/         I<Name>Repo
├─ Repositories/       <Name>Repo
├─ Validators/         Add<Name>Validator          FluentValidation
├─ Profiles/           <Name>Profile               manual mapping extension methods (ToDto)
├─ Configurations/     <Name>Configuration         IEntityTypeConfiguration
├─ Data/               <Module>DbContext           own schema
├─ Migrations/
├─ Resources/          localized messages (ar, en)
└─ <Module>Module.cs   Add<Module>Module(): registers repos, DbContext, controllers

Shifa.<Module>.Contracts/
├─ Events/             integration events other modules can handle
└─ I<Module>Api.cs     queries other modules can call
```

Shared code follows the HRM names too:

- `Shifa.Core`: `Common/` (`Response`, `BusinessException`, `PaginationParameters`, `SortItems`), `Pagination/` (`PagedList`), `Entities/` (`BaseModel`), `Interfaces/` (`ICurrentUser`, `ICacheService`, ...), `Extensions/`, `Resources/`.
- `Shifa.Infrastructure`: `Caching/`, `Common/CurrentUserManager/`, `Services/` (outbox, background jobs, files, email, SMS).
- `Shifa.API`: `Middleware/`, `Extensions/`, `Program.cs`.

### Request flow and responses

- `Controller → I<Name>Repo → <Name>Repo → Response`, as in HRM.
- `Response` is the same class as HRM: `Message`, `Status`, `Data`, `IsSuccess`. The HTTP status code always equals `Response.Status`.
- Business errors throw `BusinessException(message, status)`. `GlobalExceptionHandler` turns it into a `Response`.
- Unexpected exceptions return 500 with a generic localized message. The exception details go to the logs only, never to the client.
- `GlobalExceptionHandler` is the first middleware in the pipeline so it catches errors from every later middleware.

### Keeping repositories small

- State changes with rules live on the entity (e.g. `appointment.CheckIn(now, queueNumber)` throws `BusinessException` when the status doesn't allow it). The repo loads, calls the entity, saves.
- Mapping is manual extension methods in `Profiles/` (e.g. `doctor.ToDto()`): mapping errors show at compile time and nothing needs registering in `Program.cs`.
- When a repo passes ~400 lines, split it by area (e.g. `AppointmentBookingRepo`, `AppointmentQueueRepo`).
- `Program.cs` calls one `Add<Module>Module()` per module.

## Consequences

- Anyone who knows HRM can find their way in a module immediately.
- Module boundaries (ADR 0001) and the outbox (ADR 0005) fix the problems listed above without changing the team's style.
- `Response.Data` is `dynamic`, so OpenAPI can't describe response bodies. Accepted to keep the same contract as the other Trio APIs; `[ProducesResponseType]` can document important endpoints later.

## Alternatives considered

- **Vertical slices (feature folders with handlers):** good for large codebases, but a new style for the team with little gain at this size. Rejected.
- **Full Clean Architecture per module (Domain / Application / Infrastructure / API projects):** about 30 projects for one developer. Rejected.
- **MediatR and AutoMapper:** both moved to commercial licensing in 2025 (AutoMapper 14 is the last free version); they add indirection and registration we don't need. Rejected.
