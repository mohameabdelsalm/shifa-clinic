# ADR 0005 — Integration events through a transactional outbox

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

Modules must react to each other without direct calls. For example, when an appointment is booked, Notifications sends a confirmation and schedules reminders; when a patient checks in, Billing creates an invoice. If we publish an event after `SaveChanges` and the process crashes in between, the event is lost and the data is inconsistent. If we publish before saving and the save fails, we notify about something that never happened.

## Decision

- A module that changes state writes its integration events to its own `<schema>.outbox_messages` table **in the same transaction** as the data change.
- An `OutboxProcessor` (`BackgroundService`, polling every few seconds) reads unprocessed rows, publishes them through `IEventBus`, and marks them processed. Failures are retried with an attempts counter.
- Consumers record handled event IDs in `<schema>.inbox_messages` so a re-delivered event is processed once (idempotent consumers).
- Event contracts live in `Shifa.<Module>.Contracts` (e.g. `AppointmentBooked`, `PatientCheckedIn`, `VisitCompleted`).
- Phase 1: `IEventBus` is in-process. Phase 2: it is backed by RabbitMQ without changing module code.

## Consequences

- Events are never lost and never published for rolled-back changes.
- Delivery is at-least-once, so every consumer must be idempotent (the inbox handles this).
- Consumers see changes a few seconds later (eventual consistency). Flows that need an immediate answer use `I<Module>Api` instead.

## Alternatives considered

- **Direct module-to-module calls:** tight coupling, and a failure halfway leaves partial data. Rejected.
- **Publish after `SaveChanges` without an outbox:** events lost on crash. Rejected.
- **Distributed transactions:** not available across a broker and heavy where they are. Rejected.
