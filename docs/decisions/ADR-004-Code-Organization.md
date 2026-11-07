# ADR-004 — Layered modules with the Trio HRM conventions

- **Status:** Accepted
- **Date:** 2026-11-07
- **Decided by:** product owner, 2026-11-07

## Context

The master architecture prompt lists *Vertical Slice Architecture* for code organization. The product owner chose to keep the conventions of the team's Trio ERP HRM API instead (same folder names, `Repo` classes, `Response` shape), so every developer who knows HRM can work in Shifa immediately. The prompt also says to follow existing conventions; this ADR records the deviation explicitly.

## Decision

Inside each module project, HRM folder names and the HRM request flow:

```
Shifa.<Module>/
├─ Controllers/      <Name>Controller          thin: calls the repo, returns Response
├─ Entities/         <Name>                    entities with methods that guard business rules
├─ Enums/            <Name>StatusEnum, ...
├─ DTO/              <Name>Dto, Search<Name>sDto                     output
├─ Models/           Add<Name>Model, Update<Name>Model, Search<Name>sModel   input
├─ Interfaces/       I<Name>Repo
├─ Repositories/     <Name>Repo
├─ Validators/       Add<Name>Validator        FluentValidation
├─ Profiles/         <Name>Profile             manual mapping extension methods
├─ Configurations/   <Name>Configuration       IEntityTypeConfiguration
├─ Data/             <Module>DbContext
├─ Migrations/
├─ Resources/        localized messages (en, ar)
└─ <Module>Module.cs Add<Module>Module()
```

- Flow: `Controller → I<Name>Repo → <Name>Repo → Response`.
- `Response` has the HRM shape (`Message`, `Status`, `Data`, `IsSuccess`); the HTTP status code equals `Response.Status`.
- Business errors: `throw new BusinessException(message, status)`; `GlobalExceptionHandler` converts it. Unexpected errors return 500 with a generic localized message.
- **Lightweight CQRS inside repos:** read methods project to DTOs with `AsNoTracking` and never load entities for display; write methods load entities, call entity methods that enforce rules, and save.
- Manual mapping in `Profiles/` (no AutoMapper). No MediatR. No generic repository.
- A repo that passes ~400 lines is split by area (e.g. `AppointmentBookingRepo`, `AppointmentQueueRepo`).
- Shared code uses HRM names: `Shifa.Core` (`Common/`, `Entities/BaseModel`, `Enums/`, `Interfaces/`, `LanguageResource`), `Shifa.Infrastructure` (`Common/CurrentUserManager`, `Data/`, `Caching/`, `Services/`).

## Consequences

- Familiar structure; one interface per repo is accepted as the team's convention.
- The HRM problems (800-line repos, rules spread across repos, publish-after-save) are prevented by entity methods, the repo size rule and the outbox, not by changing the style.
- `Response.Data` is untyped, so OpenAPI cannot describe response bodies automatically; important endpoints document examples with `[ProducesResponseType]` and Swagger examples.

## Alternatives considered

- **Vertical slices (master prompt):** a new style for the team with little gain at this size. Not chosen by the product owner.
- **Full Clean Architecture per module:** ~4 projects per module. Rejected.
