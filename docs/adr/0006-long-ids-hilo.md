# ADR 0006 — `long` IDs generated with HiLo

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

The Trio ERP APIs use `long` (`bigint`) IDs, and the team prefers to keep that. With a plain identity column the ID only exists after the row is inserted. That complicates the outbox (ADR 0005): an event like `AppointmentBooked` needs the appointment ID and must be saved in the same `SaveChangesAsync` call as the appointment.

## Decision

- Primary keys are `long` (`bigint`).
- They are generated with EF Core's HiLo algorithm on a SQL Server sequence in the module's own schema (`UseHiLo`). EF Core reserves a block of IDs from the sequence, so an entity has its ID as soon as it is added, before `SaveChangesAsync`.
- IDs of outbox and inbox messages are `Guid`: an event ID must be unique across all modules.

## Consequences

- Same ID type the team already uses; small, ordered clustered indexes.
- Gaps in ID values are normal (unused parts of a reserved block). IDs must never be used as counters or invoice numbers.
- `long` IDs are guessable (`/api/patients/1234` → try 1235). Every endpoint that returns patient data must check access: a patient sees only their own records, and staff need the right permission. Integration tests cover this.
- When a module becomes its own service, its schema and sequence move with it unchanged.

## Alternatives considered

- **`Guid` IDs:** not guessable and easy to generate anywhere, but different from the team's other APIs. Rejected for now; can be revisited for patient-facing IDs.
- **Identity columns:** the ID is only known after insert, so the outbox would need a second save inside an explicit transaction. Rejected.
