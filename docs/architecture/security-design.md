# Security design

Authentication and authorization model: ADR-007. Tenant isolation: [tenancy-design.md](tenancy-design.md).

## Threats we design against

| Threat | Control |
|---|---|
| Cross-tenant access | Tenant from validated token only; query filters; save interceptor; composite FKs; integration tests |
| IDOR (guessing `long` IDs) | Every read/write by ID goes through the filtered context **and** branch/patient checks; 404 (not 403) for rows outside scope |
| Privilege escalation | Permission catalog in code; role changes audited; Platform Admin is a separate policy |
| Stolen tokens | 15-min access tokens, rotating refresh tokens stored hashed, reuse detection revokes the chain |
| Brute force / OTP abuse | ASP.NET Core rate limiter on login, OTP and public booking; OTP hashed, max attempts, short expiry |
| Data leaks in logs | Serilog destructuring policy; never log bodies, medical data, national IDs, passwords, OTPs, tokens |
| Error leaks | `GlobalExceptionHandler` returns a generic message for unexpected errors; details only in logs |
| Malicious files | Allow-listed content types and extensions, size limit, magic-byte check, stored privately, served via short-lived signed URLs after an ownership check |
| Silent medical record changes | Completed visits are immutable; addendums with author, time, reason; record access audited |

## Authorization checks per request

1. Authenticated (JWT valid, not expired).
2. Tenant context set from a validated, active membership in an active tenant.
3. Permission required by the endpoint (`[Permission("Code")]`).
4. Branch scope: the target branch is in the membership's branches (or `AllBranches`).
5. Patient scope (portal): the patient is linked to the user by `PatientAccountAccess`.
6. Entity state rules (e.g. only the visit's doctor edits an open visit).

## Audit

`identity.AuditLogs` records: sign-ins and failures, role and permission changes, membership changes, Platform Admin cross-tenant access, medical record views and changes, exports, invoice issue/void, refunds. Audit rows are append-only.

## Data protection

- TLS everywhere; HSTS in production.
- Database encryption at rest (TDE on licensed SQL Server or Azure SQL) and encrypted backups. Details in Phase 10.
- Secrets: `user-secrets` locally, a secret store in production. Nothing secret in committed files.
- Synthetic data only in development and tests.
- Data retention and deletion policy per record type — needs legal input (see `project/assumptions.md`).

## Legal

Egyptian Personal Data Protection Law No. 151 of 2020 and its Executive Regulations apply to patient data. Technical controls here support compliance but do not by themselves make the product compliant. Items needing legal review (consent wording, health-data handling requirements, cross-border transfer, retention periods, breach notification, data subject requests) are tracked in `project/assumptions.md`.
