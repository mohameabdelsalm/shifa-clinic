# ADR 0001 — Start as a modular monolith

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

One developer is building the system. The domain boundaries (scheduling vs. clinical vs. billing) are reasonable guesses but not yet proven. The client's load (~400 visits/day) does not need independent scaling of any part. We still want the option to split into microservices later.

## Decision

Build one deployable ASP.NET Core application (`Shifa.Api`) made of modules with strict boundaries:

- Modules: Identity, Clinic, Patients, Scheduling, Clinical, Billing, Notifications.
- Each module is two projects: `Shifa.<Module>` (internal implementation) and `Shifa.<Module>.Contracts` (public integration events and module API interfaces).
- Each module owns its SQL Server schema, its `DbContext` and its migrations.
- No foreign keys or joins across schemas; other modules' data is referenced by ID only.
- Modules talk synchronously only through `I<Module>Api` interfaces in Contracts, and asynchronously through integration events (see ADR 0005).
- Architecture tests fail the build when a rule is broken.

## Consequences

- One process to run, debug and deploy; no network calls between modules; local transactions.
- Extracting a module later means: move its schema to its own database, host it separately, and replace the in-process event bus and `I<Module>Api` calls with a broker and HTTP. No business code changes.
- The boundary rules add some ceremony (Contracts projects, events instead of direct calls). This is the price of keeping the split cheap.

## Alternatives considered

- **Microservices from day one:** network failures, distributed transactions, service discovery and per-service deployment before there is any business. Rejected.
- **Classic layered monolith (one project per layer):** fastest to start, but modules leak into each other and splitting later becomes a rewrite. Rejected.
