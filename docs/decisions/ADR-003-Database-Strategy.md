# ADR-003 — SQL Server, schema per module, long HiLo keys

- **Status:** Accepted
- **Date:** 2026-11-07

## Context

The data is relational (patient → appointment → visit → invoice) and booking and payments need transactions and strong constraints. The team uses SQL Server in the Trio ERP APIs and `long` IDs.

## Decision

- SQL Server with EF Core 10. One database, one schema per module (lowercase module name). Each module has its own migrations history table in its schema.
- Table and column names follow EF Core defaults (PascalCase, as in the Trio APIs).
- Keys: `long` (`bigint`) generated with EF Core **HiLo** on a sequence in the module's schema, so an entity has its ID before `SaveChanges` (needed to write outbox events in the same save).
- Tenant-owned tables: unique `(TenantId, Id)` alternate key; foreign keys between tenant-owned tables are composite `(TenantId, ParentId)` (ADR-002).
- Money: `decimal(18,2)`; never floating point.
- Time: `datetimeoffset`, stored in UTC. Schedules use the branch's time zone (`TimeZoneId`, IANA).
- Optimistic concurrency with `rowversion` on entities edited concurrently.
- Soft delete (`IsDeleted`) with a named query filter `SoftDelete`. Medical and financial records are never hard-deleted.
- Double booking is prevented by `scheduling.AppointmentSlots` with a unique key on `(TenantId, DoctorId, SlotStart)` (SQL Server has no exclusion constraints).
- Nullable unique columns use filtered unique indexes (`WHERE ... IS NOT NULL`).
- Migrations are applied by a deployment step, not by the application at startup. Nothing drops or recreates a database automatically.
- Local development uses SQL Server 2022 Developer Edition (`Server=.`). Production must use a licensed edition (Standard, Enterprise) or Azure SQL; Developer Edition is not licensed for production.

## Consequences

- Familiar stack and IDs for the team; mature tooling.
- `long` IDs are guessable, so every endpoint checks tenant, branch and patient access (IDOR protection is mandatory, see [security-design.md](../architecture/security-design.md)).
- HiLo leaves gaps in ID values; IDs are never used as counters or document numbers.

## Alternatives considered

- **PostgreSQL:** free, has exclusion constraints; new to the team. Rejected for now.
- **Guid keys:** not guessable, but differ from the team's conventions. Rejected; may be revisited for patient-facing identifiers.
- **Identity columns:** ID only known after insert, complicating the outbox. Rejected.
