# Implementation roadmap

Each phase ends with: build green, relevant tests green, docs updated, `project/progress.md` updated, merged to `develop`. Model guidance uses Claude Code aliases.

| Phase | Scope | Model / effort | Exit criteria |
|---|---|---|---|
| 0 | Architecture finalization: ADRs, architecture docs, revised ERD, assumptions | opus / high | Docs in `docs/` and `project/` |
| 1 | Foundation: solution, DI, configuration, Serilog, exception handling, validation, Swagger, health checks, module registration, tenant context, data-layer isolation, architecture tests, initial migrations | sonnet / medium | Builds; architecture + SQL Server isolation tests pass |
| 2 | Identity, Tenancy, Platform Admin: users, memberships, JWT + refresh, roles/permissions, branch scope, tenants, plans, subscriptions, entitlements, membership validation in tenant resolution | sonnet / medium → **opus / high review** | Cross-tenant read/update/delete tests fail closed |
| 3 | Clinic setup: branches, doctors, specialties, services, branch prices, doctor branches, schedules, time off | sonnet / medium | Branch scope + isolation tests |
| 4 | Patients: registration, identifiers, guardians, account access, consent, search | sonnet / medium | Patient access tests |
| 5 | Scheduling: availability, slot reservation, booking, cancel, reschedule, check-in, queue, no-show, idempotency | **opus / high design** → sonnet / medium → **opus / high review** | Concurrent booking test against SQL Server proves no overlap; idempotency tests |
| 6 | Clinical / EMR: visits, notes, vitals, diagnoses, prescriptions, allergies, conditions, attachments, addendums | sonnet / medium → **opus / high review** | Immutability + access audit tests |
| 7 | Billing: invoices, items, payments, partial payments, refunds, credit notes, cashier shifts | **opus / high design** → sonnet / medium | Duplicate payment and totals consistency tests |
| 8 | Outbox, dispatcher, inbox, Hangfire jobs, notification templates, delivery attempts, reminders — **done 2026-11-18 (ADR-015)** | sonnet / medium | Duplicate event + worker failure tests |
| 9 | Reporting: daily branch stats, appointments, revenue vs collections, doctor stats | sonnet / medium; haiku / low for docs | Report scope tests |
| 10 | Production readiness: OpenTelemetry, security review, performance, index review, backup/restore, deployment, configuration | sonnet / medium → **opus / high final review** | Critical tests and operational checks pass |

## Phase 1 detail

1. Shared building blocks with HRM names: `Response`, `BusinessException`, `BaseModel`, `StatusEnum`, `LanguageResource` (en, ar), `ICurrentUser`.
2. Tenancy foundation: `ITenantContext` (scoped, set once), `TenantOwnedEntity`, `[PlatformOwned]`, `TenantResolutionHandler` (claim → context).
3. Data foundation: `ModuleDbContext` (schema, HiLo, `Tenant` and `SoftDelete` named filters, `(TenantId, Id)` keys, decimal precision), `AuditTenantInterceptor`.
4. API: Serilog, localization, `GlobalExceptionHandler`, model-state and FluentValidation responses in HRM `Response` shape, Swagger (Development), health checks `/health/live` and `/health/ready`.
5. Modules: Tenancy (`Tenant`) and Clinic (`Branch`) with initial migrations, proving platform-owned and tenant-owned entities.
6. Tests: architecture (references, ownership, composite FKs), integration on SQL Server (isolation, stamping, guards, HiLo, soft delete, health).
7. `docker-compose.yml` for Redis, Seq, Mailpit, Azurite.

## Phase 5 detail (ADR-008)

1. **Clinic prerequisites:** schedule validator — start/end/slot multiples of 5 minutes; `IClinicApi.GetBookingInputsAsync(branchId, doctorId, serviceId)` and `GetDoctorTimeOffAsync(doctorId, fromUtc, toUtc)`.
2. **Scheduling module:** `Appointment`, `AppointmentSlot` (5-minute ticks), `AppointmentStatusHistory`, `IdempotencyRecord`; `Shifa.Scheduling.Contracts` with the event records.
3. **`AvailabilityCalculator`** (pure, unit-tested incl. a DST transition and multi-slot services) + availability endpoints (staff and portal).
4. **Booking:** `POST api/appointments` and `api/portal/appointments` with `Idempotency-Key`; one `SaveChanges`; unique violations → 409 / idempotent replay.
5. **Lifecycle:** confirm, check-in (+ queue number), cancel (portal 3 h rule), no-show, reschedule (release then claim in one transaction); `rowversion` → 409.
6. **Lists/views:** search by date range, branch (scope), doctor, patient, status; portal list of linked patients' appointments.
7. **Tests:** N parallel bookings of one slot → exactly one wins; overlapping multi-slot and cross-branch bookings; idempotent replay and key reuse; cancel frees ticks; reschedule onto own ticks; status-change race; branch scope and tenant isolation; portal `CanBook`.
8. **Review:** opus / high on the booking transaction, the calculator and the tests.

## Phase 7 detail (ADR-011)

1. **Prerequisites:** `IClinicApi.GetCurrentPriceAsync(serviceId, branchId)`; booking refuses a service with no current price at the branch; `Billing.Read`/`Billing.Collect` added to the default Receptionist role.
2. **Billing module:** `Invoice` (rowversion, amount invariant in entity methods), `InvoiceItem`, `Payment`, `Refund`, `CreditNote`, `CashierShift`, `IdempotencyRecord`; numbering (`INV-`, `RCT-`, `RFD-`, `CN-`) with `DatabaseLocks`; migration.
3. **Shifts:** open (one per user and branch), current shift, close with counted cash (expected cash and variance), list/view per branch and day.
4. **Invoices:** `IBillingApi.EnsureVisitInvoiceAsync` called by check-in (first and repeated); `POST api/invoices/visit/{appointmentId}` repair; extra invoices from the price list; search/view in branch scope; void.
5. **Money:** payments (`Idempotency-Key`, ≤ balance, open shift, reference for Card/Wallet), refunds, credit notes; all through the invoice rowversion.
6. **Portal and statistics:** `GET api/portal/invoices`; `POST api/dashboard/payments` (collections by day/month and method, refunds, net) and Revenue/Collections KPIs in `api/dashboard/statistics`.
7. **Verification:** HTTP smoke on `Shifa_Smoke`: check-in creates one invoice; parallel duplicate payments with one key → one payment; parallel different payments can't overpay; refund/credit/void rules; shift totals and variance; branch scope; portal isolation.
8. **Review:** opus / high on the money paths and concurrency.
