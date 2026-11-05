# ADR 0002 — SQL Server with one schema per module

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

We need a relational database: the data is relational by nature (patient → appointment → visit → invoice) and booking and payments need transactions and strong constraints. The team already uses SQL Server (the Trio ERP Auth API runs on it), and SQL Server 2022 Developer Edition is installed on the development machine.

## Decision

- Use SQL Server 2022 with EF Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`).
- Locally, use the default instance `.` with Windows Authentication:
  `Server=.;Database=Shifa;Trusted_Connection=True;TrustServerCertificate=True`
- One database, one schema per module (lowercase module name).
- Hangfire uses SQL Server as its job storage.

SQL Server-specific choices this forces:

- **Double booking:** SQL Server has no exclusion constraints, so each appointment writes rows to `scheduling.appointment_slots` with primary key `(doctor_id, slot_start)`. The database rejects a second booking of the same slot.
- **IDs:** `bigint` generated with EF Core HiLo sequences per schema (ADR 0006).
- **Nullable unique columns** (`national_id`, `email`) use filtered unique indexes (`WHERE ... IS NOT NULL`).
- **Optimistic concurrency** uses a `rowversion` column.
- **Times** are `datetimeoffset` in UTC.

## Consequences

- No new database to learn; mature tooling (SSMS, EF Core provider, Hangfire storage).
- Production licensing is decided at deployment time: Express (free, with size and resource limits), Standard, or Azure SQL.
- The slot table adds one write per slot on booking. At our volume this is negligible.

## Alternatives considered

- **PostgreSQL:** free, has exclusion constraints and `jsonb`. Rejected because it is new to the team; we can revisit if production licensing becomes a problem.
- **MongoDB:** the data is relational and needs multi-row transactions and constraints. Rejected.
