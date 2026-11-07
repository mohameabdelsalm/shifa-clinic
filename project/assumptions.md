# Assumptions and open questions

Safe defaults used until the product owner or a legal/business reviewer decides otherwise. Change the code and this file together.

## Business assumptions (defaults in use)

| # | Assumption | Default | Owner to confirm |
|---|---|---|---|
| A1 | A person visiting two tenants | Two independent patient profiles; no cross-tenant medical history | Product |
| A2 | User accounts | Global (one per person, by phone or email); tenant access via memberships | Product |
| A3 | Patient portal tenant selection | The patient picks the clinic (tenant slug); the token is issued for that tenant | Product |
| A4 | Currency | One currency per tenant, default EGP; no multi-currency invoices | Product |
| A5 | Plan pricing | Stored as data on `Plan`; no hardcoded prices; subscription payments recorded manually (no gateway chosen) | Product |
| A6 | Subscription expiry | Expired/suspended tenant: staff can sign in read-only for 30 days, no new bookings | Product |
| A7 | Default roles on onboarding | TenantAdmin, BranchManager, Doctor, Receptionist, Cashier, Nurse | Product |
| A8 | Draft invoice timing | Created at check-in, finalized when the visit is completed | Product |
| A9 | Cancellation window | Patients can cancel online up to 3 hours before start (tenant setting) | Product |
| A10 | No-show rule | Confirmed and not checked in 30 min after start → NoShow (tenant setting) | Product |
| A11 | Doctor double booking | A doctor cannot have overlapping appointments across all branches of the tenant | Product |
| A12 | Slot grid | Per doctor schedule (`SlotMinutes`); service duration is rounded up to whole slots | Product |
| A13 | Time zones | Each branch has an IANA time zone; tenant default `Africa/Cairo` | Product |

## Needs legal / regulatory review

| # | Topic | Why |
|---|---|---|
| L1 | Egyptian PDPL (Law 151/2020) and Executive Regulations: health data as sensitive data, licensing/registration duties for controllers/processors | Determines consent, registration and processing obligations |
| L2 | Consent wording and records for patients and guardians of minors | `PatientConsent` model must match legal requirements |
| L3 | Medical record retention periods and deletion rules | Drives retention jobs and soft-delete policy |
| L4 | Data hosting location and cross-border transfer | Affects the choice of cloud region |
| L5 | Breach notification duties and timelines | Incident response plan |
| L6 | Egyptian tax rules and e-invoicing / e-receipt obligations for clinics | Invoice and tax fields are placeholders until confirmed |
| L7 | Prescription legal requirements (signatures, controlled drugs) | Prescription model and printing |

## Technical assumptions

| # | Assumption |
|---|---|
| T1 | Expected scale for v1: tens of tenants, each up to ~500 visits/day; single SQL Server database is sufficient |
| T2 | Docker Desktop is available locally for Redis, Seq, Mailpit, Azurite (not required for build and tests in Phase 1) |
| T3 | Integration tests use the local SQL Server instance (`Shifa_Tests`); Testcontainers when CI exists |
