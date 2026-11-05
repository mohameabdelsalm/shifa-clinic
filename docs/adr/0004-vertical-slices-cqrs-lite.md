# ADR 0004 — Vertical slices and CQRS-lite inside each module

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

In the Trio ERP Auth API, one repository class per entity does validation, mapping, business rules and data access. These classes grow large and the rules are hard to find and test. We want each feature easy to locate and the business rules in one place.

## Decision

Inside each module:

```
Shifa.<Module>/
├─ Domain/              aggregates, value objects, domain errors (business rules live here)
├─ Features/
│  └─ <FeatureName>/    Endpoint · Command or Query · Handler · Validator
├─ Infrastructure/      DbContext, entity configurations, migrations, external adapters
└─ <Module>Module.cs    Add<Module>Module() + Map<Module>Endpoints()
```

- **Commands** load an aggregate, call a domain method that returns `Result`, and save through the module's `DbContext` (the unit of work).
- **Queries** project straight to response DTOs with `AsNoTracking`. They never load aggregates.
- Handlers are plain classes injected into Minimal API endpoints. **No MediatR.**
- Mapping is manual (or Mapperly if it becomes repetitive). **No AutoMapper.**
- **No generic repository.** EF Core's `DbContext` already is the repository and unit of work.
- Validation with FluentValidation. Business errors return `Result`/`Error` and become ProblemDetails (RFC 9457) with the right HTTP status.

## Consequences

- One folder per feature; small, testable handlers; domain rules unit-testable without a database.
- No commercial-license dependencies: MediatR and AutoMapper moved to commercial licensing in 2025.
- Cross-cutting behaviour (logging, validation) is done with endpoint filters and decorators instead of a mediator pipeline.

## Alternatives considered

- **Repository per entity (as in the Auth API):** mixes concerns and grows without limit. Rejected.
- **MediatR pipeline:** familiar, but adds a licensed dependency and indirection we don't need. Rejected.
- **Generic repository + unit of work over EF Core:** hides EF features (projection, `Include`) and adds no value. Rejected.
