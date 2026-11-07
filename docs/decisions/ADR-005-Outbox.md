# ADR-005 — Transactional outbox for integration events

- **Status:** Accepted
- **Date:** 2026-11-07

## Context

Modules react to each other's changes (an appointment is booked → reminders are scheduled; a patient checks in → a draft invoice is created). Publishing after `SaveChanges` loses the event if the process crashes in between; publishing before can announce a change that was rolled back.

## Decision

- A module writes integration events to `<schema>.OutboxMessages` **in the same `SaveChanges`** as the business change. Each message carries `TenantId`.
- An `OutboxDispatcher` (`BackgroundService`) polls unprocessed messages, dispatches them to in-process handlers, and marks them processed. Failures are retried with backoff; after N attempts a message is marked poisoned and logged for investigation.
- Consumers record handled message IDs in `<schema>.InboxMessages` so a redelivered event runs once (idempotent consumers). Every consumer sets the tenant context from the message before touching data.
- Event contracts live in `Shifa.<Module>.Contracts/Events`.
- Hangfire runs scheduled and delayed jobs (reminders, cleanup, reports). It is not used as a substitute for the outbox.
- No RabbitMQ until a real requirement (e.g. an extracted service) needs it; the dispatcher's transport can be swapped then.

## Consequences

- Events are never lost and never published for rolled-back changes.
- Delivery is at-least-once, so consumers must be idempotent; the inbox handles this.
- Consumers see changes a few seconds later. Flows that need an immediate answer use `I<Module>Api`.

## Alternatives considered

- **Direct module-to-module calls for side effects:** coupling and partial failure. Rejected.
- **Message broker now:** extra infrastructure with no current need. Rejected.
