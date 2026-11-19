# ADR-002 — Shared database, TenantId-based isolation

- **Status:** Accepted
- **Date:** 2026-11-07

## Context

Every customer organization (tenant) must see only its own data. We must onboard tenants without code changes or new deployments, at a reasonable infrastructure cost. Medical data makes a cross-tenant leak a severe incident.

## Decision

- One SQL Server database for all tenants. Tenant-owned rows carry `TenantId`.
- Isolation is enforced in layers (details in [tenancy-design.md](../architecture/tenancy-design.md)):
  1. The effective tenant comes from the authenticated user's validated membership, never from the request body or query string.
  2. A scoped, set-once `ITenantContext` per request or job. No static or singleton mutable tenant state.
  3. EF Core named query filter `Tenant` on every tenant-owned entity.
  4. A save interceptor stamps `TenantId` on inserts and rejects writes to another tenant's rows or changes to `TenantId`.
  5. Composite keys: every tenant-owned table has a unique `(TenantId, Id)` key; foreign keys between tenant-owned tables include `TenantId`, so the database rejects cross-tenant references.
  6. Authorization policies for tenant scope and branch scope.
  7. Integration tests that try cross-tenant reads and writes.
- Every entity declares its ownership: tenant-owned (`TenantOwnedEntity`) or platform-owned (`[PlatformOwned]`). An architecture test fails if an entity has neither.
- Platform Admin cross-tenant access is a separate authorization boundary and is audited.

## Consequences

- One schema to migrate and back up; onboarding a tenant is inserting rows.
- Isolation depends on application code plus database keys. Query filters alone are not trusted: raw SQL, background jobs, cache keys, files and reports each have explicit rules.
- A very large tenant could later move to its own database (database-per-tenant for selected tenants) because every row already has `TenantId`.

## Alternatives considered

- **Database per tenant:** strongest isolation, but migrations, backups and connections multiply per customer from day one. Rejected for v1.
- **Schema per tenant:** same operational cost as database-per-tenant with weaker tooling. Rejected.
- **Separate deployment per customer** (the 2026-11-05 plan): contradicts the SaaS model. Superseded.
