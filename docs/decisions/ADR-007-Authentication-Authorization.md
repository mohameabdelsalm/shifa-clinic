# ADR-007 — Authentication and authorization model

- **Status:** Accepted
- **Date:** 2026-11-07

## Context

Users include platform staff (the product owner), tenant staff with different roles and branches, and patients or guardians using a portal. One person may belong to more than one tenant. A wrong decision here leaks data across tenants, branches or patients.

## Decision

- **Authentication:** ASP.NET Core Identity for users and password hashing; short-lived JWT access tokens (15 min) and rotating refresh tokens stored hashed. Patients may sign in with phone OTP. Users are global (one account per person); access to a tenant comes from a membership.
- **Tenant-scoped tokens:** an access token is issued for one tenant (`tenant_id`, `membership_id` claims). Switching tenant issues a new token. On every request the server validates that the membership and the tenant are active (cached briefly) before setting `ITenantContext`.
- **RBAC:** permissions are a code-defined catalog (`Appointments.Create`, `Clinical.Read`, ...). Roles are per tenant and group permissions; default roles (TenantAdmin, BranchManager, Doctor, Receptionist, Cashier, Nurse) are created when a tenant is onboarded. Endpoints require permissions via `[Permission("...")]`, as in the Trio Auth API.
- **Branch scope:** a membership has either all-branches access or an explicit branch list. Branch-scoped operations check it on the server for every request.
- **Patient scope:** a portal user sees only patient profiles linked to them by `PatientAccountAccess` (self, child, ward) inside the current tenant.
- **Platform scope:** Platform Admin is a separate claim and policy, with endpoints under `api/platform/...`. Cross-tenant data access is explicit (`IgnoreQueryFilters` on a named filter) and always audited.

## Consequences

- One login per person, with explicit tenant selection; a token can never act on two tenants.
- Membership and tenant status are checked per request, so suspending a tenant takes effect within the cache TTL.
- Every repo method that reads by ID must check tenant (query filter + key), branch and, for patient data, patient access.

## Alternatives considered

- **Tenant ID in a request header:** the client controls it. Rejected (only allowed as a hint to pick which membership to issue a token for, at login).
- **External identity provider now (Keycloak, Entra ID):** more infrastructure than needed for v1; can be adopted later behind the same claims.
