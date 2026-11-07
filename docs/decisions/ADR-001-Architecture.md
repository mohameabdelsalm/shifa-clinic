# ADR-001 — Multi-tenant modular monolith

- **Status:** Accepted (supersedes the single-tenant plan of 2026-11-05)
- **Date:** 2026-11-07

## Context

Shifa is a commercial SaaS product sold to many clinic organizations. One developer builds it. Module boundaries (scheduling, clinical, billing, ...) are reasonable but not proven, and expected load does not need independent scaling of any part.

## Decision

- One deployable ASP.NET Core application (`Shifa.API`, .NET 10) serving all tenants.
- Business code is split into modules with strict boundaries: Identity, Tenancy, Clinic, Patients, Scheduling, Clinical, Billing, Notifications, Reporting.
- Each module = `Shifa.<Module>` (implementation) + `Shifa.<Module>.Contracts` (public interfaces and integration events).
- Each module owns its SQL Server schema, `DbContext` and migrations. No foreign keys or joins across schemas.
- Modules talk through `I<Module>Api` interfaces (synchronous queries) and integration events through the transactional outbox (ADR-005).
- No microservices, no message broker, no per-module deployments now.
- Architecture tests enforce the boundaries.

## Consequences

- One process to run, debug, deploy and monitor; local transactions inside a module.
- A module can later move to its own service by moving its schema and replacing in-process calls/events with HTTP and a broker. No business code rewrite.
- Boundary ceremony (Contracts projects, events) is the price of keeping that option cheap.

## Alternatives considered

- **Microservices now:** distributed transactions, network failures and operational cost with no business need. Rejected.
- **Layered monolith without module boundaries:** fastest to start, but modules leak into each other (seen in the Trio HRM API) and extraction becomes a rewrite. Rejected.
