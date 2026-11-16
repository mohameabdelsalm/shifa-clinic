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
| A6 | Subscription expiry | Expired/suspended tenant: staff can sign in read-only for 30 days, no new bookings. Implemented for expiry in ADR-014 (suspension still blocks at once) | Product |
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

## Added in Phase 4

| # | Assumption | Default | Owner to confirm |
|---|---|---|---|
| A14 | Phone numbers without a country code | A single leading 0 means a local number of the default country, Egypt (+20); numbers are stored as `+` and 8–15 digits | Product |
| A15 | Patient portal access | Created by clinic staff (a phone number linked to each patient); patients can't self-register yet. A phone number is linked per clinic | Product |
| A16 | Under-18 patients | At least one guardian is required at registration and the last guardian can't be removed; age is computed from the UTC date | Product / Legal |
| A17 | MRN format | `MRN-000123`, a running number per tenant, never reused | Product |
| A18 | Duplicate patients | Detected by identical national ID or passport (same type) within a tenant; no fuzzy name matching yet | Product |

Legal items L1 and L2 (consent wording, guardians of minors) now matter: the system records *that* a consent was given and when, but the text shown to the patient and who may consent for a minor still need legal review.

## Added in Phase 5 (design)

| # | Assumption | Default | Owner to confirm |
|---|---|---|---|
| A19 | Booking horizon and check-in window | Bookable up to 90 days ahead; check-in from 2 hours before the start until the end of that local day | Product |
| A20 | Schedule grid | Schedule times and slot lengths are multiples of 5 minutes (ADR-008) | Product |
| A21 | Service longer than a slot | Rounded up to whole slots; it must fit inside one schedule window (no booking across a break) | Product |
| A22 | DST | Local times that don't exist are not offered; ambiguous local times use the first occurrence | Product |
