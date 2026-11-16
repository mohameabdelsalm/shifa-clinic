# ADR-011 — Billing: invoices, payments, refunds, credit notes, cashier shifts

- **Status:** Accepted
- **Date:** 2026-11-16
- **Decided by:** product owner, 2026-11-16 (payment timing, discounts, payment methods, shifts)

## Context

Phase 7 turns visits into money: an invoice per visit, partial payments, refunds, corrections and a daily cash close per branch (`docs/vision.md`: "money leaks between the visit and the cash desk"; rule 8: the invoice uses the service price at check-in). Money code must never lose, double-count or silently change an amount, also under concurrent requests and retries. Tax and e-receipt rules in Egypt are not confirmed yet (assumption L6).

## Decisions of the product owner

1. **Pay on arrival.** The visit invoice is issued (final, numbered, payable) when the patient checks in; reception collects before the patient sees the doctor. Anything extra gets its own invoice.
2. **No discounts in this version.** Prices always come from the branch price list; nobody can type a price. (`Discount` stays in the schema at 0 for later.)
3. **Payment methods:** Cash, Card (POS terminal), Wallet (InstaPay / Vodafone Cash), all recorded manually. Card and Wallet need a reference number. The cash close counts cash only.
4. **Cashier shifts are mandatory.** Every payment and refund belongs to the collector's open shift at that branch.

## Design

**Module and data.** A `billing` schema with its own `BillingDbContext`: `Invoices`, `InvoiceItems`, `Payments`, `Refunds`, `CreditNotes`, `CashierShifts`, `IdempotencyRecords`. Composite tenant FKs between them; branch, patient, appointment and service are ids of other modules. Money is `decimal(18,2)`. Line totals are rounded to 2 decimals (away from zero). The tenant's currency (`Tenant.CurrencyCode`, default EGP, assumption A4) is copied onto each invoice.

**Invoice.**
- `Kind`: `Visit` (created automatically at check-in, at most one per appointment: unique `(TenantId, AppointmentId) WHERE Kind = 'Visit'`) or `Extra` (created by reception for additional services, optionally linked to the appointment).
- Items always reference a service; `UnitPrice` is a snapshot of the branch's current price (`IClinicApi.GetCurrentPriceAsync`). Quantity 1–100.
- Numbers are given at creation: `INV-{yyyy}-{000001}` per tenant and year, using `DatabaseLocks` ("max + 1" under an app lock) plus a unique index. Payments, refunds and credit notes get `RCT-`, `RFD-`, `CN-` numbers the same way.
- An invoice is never edited after it is created. Corrections are credit notes; a mistaken invoice with no money on it is voided (with a reason).
- Amounts kept on the invoice and changed only in the same save as the row that causes them: `Total`, `PaidAmount` (payments − refunds), `CreditedAmount`. `Balance = Total − CreditedAmount − PaidAmount`.
- **Invariant:** `0 ≤ PaidAmount` and `PaidAmount + CreditedAmount ≤ Total`, so no overpayment and no negative balance. The entity methods (`ApplyPayment`, `ApplyRefund`, `ApplyCredit`, `Void`) enforce it. The invoice `rowversion` makes two money operations on the same invoice serialize: the second one fails (409) and the client retries against the new balance.
- `Status` follows from the amounts: `Open` (nothing paid), `PartiallyPaid`, `Paid` (balance 0, including fully credited), `Void`.

**Check-in → invoice.** `AppointmentRepo.CheckIn` commits the check-in, then calls `IBillingApi.EnsureVisitInvoiceAsync(appointmentId)`. It is idempotent: the unique index and a lookup return the existing invoice. A repeated check-in also calls it, so a crash between the two saves is repaired by checking in again (or by `POST api/invoices/visit/{appointmentId}`). Booking already requires the service to be bookable at the branch; it will also require a current price there, so check-in can always price the visit. Phase 8's outbox can replace the direct call with the `PatientCheckedIn` event.

**Payments.** `Idempotency-Key` is required and scoped to the caller (same pattern as booking, ADR-008): a retry returns the same payment. Amount > 0 and ≤ balance. Method Cash/Card/Wallet; reference required for Card/Wallet. The caller must have an open shift at the invoice's branch. Each payment gets a receipt number.

**Refunds** (`Billing.Refund`). Against one payment, up to what is left of it, with a reason, by the same method as the payment. Also needs an `Idempotency-Key` and an open shift of the refunder at that branch (a cash refund leaves that drawer).

**Credit notes** (`Billing.Refund`). Reduce what is owed (e.g. a service not given). Amount ≤ `Total − CreditedAmount − PaidAmount`: to credit money already paid, refund first. **Void** (`Billing.Refund`): only when nothing was paid or credited.

**Cashier shifts.**
- Open: one open shift per user per branch (filtered unique index), with opening cash.
- Close: the cashier enters the counted cash. The system stores `ExpectedCash = OpeningCash + cash payments − cash refunds` and `Variance = Counted − Expected`, with totals per method.
- A closed shift never changes. A cashier closes their own shift; a `Billing.Refund` holder can close any shift in their branches (e.g. the cashier left).
- The daily close of a branch is the list of its shifts for a local day.

**Permissions and scope.**
- `Billing.Read`: invoices and shifts in the caller's branches.
- `Billing.Collect`: payments, extra invoices, opening and closing one's own shift.
- `Billing.Refund`: refunds, credit notes, void, closing others' shifts.
- The default Receptionist role gets `Billing.Read` and `Billing.Collect` (vision: reception collects payments); Cashier already has them.
- Branch scope applies to everything; another branch's or tenant's invoice → 404.

**Audit.** Every invoice creation, payment, refund, credit note, void, and shift open/close writes an audit row (`CancellationToken.None`, like Clinical).

**Portal.** `GET api/portal/invoices`: invoices of the patients linked to the signed-in phone (number, date, items, totals, paid, balance, status) in a portal DTO. Online payment is out of scope.

**Payment statistics** (requested by the product owner, 2026-11-16; same HRM dashboard pattern as `api/dashboard`, `Reports.Read`, branch scope):
- `POST api/dashboard/payments` with `DashboardModel` (+ optional `CashierUserId`): collections per day (≤ 31 days) or month, split by method (Cash, Card, Wallet), refunds, net collected; totals for the period.
- `POST api/dashboard/statistics` gains `Revenue` (invoiced − credited) and `Collections` (payments − refunds) KPIs, compared with the same period last year.
- Billing exposes compact grouped counts/sums through `IBillingApi` (grouped in SQL by day, branch and method), like `ISchedulingApi.CountAppointmentsAsync`.

**Tax.** `Tax` is stored as 0 until L6 (Egyptian tax / e-receipt rules) is confirmed. Then a tax rate per tenant and the e-receipt integration are added.

## Consequences

- No price typing and no discounts means fewer fraud paths and simpler totals; discounts later need their own permission and audit.
- Issuing at check-in means a patient who leaves before seeing the doctor has an invoice to void or credit; the void path covers it.
- Check-in and invoice creation touch two schemas without one transaction; idempotent repair keeps them consistent until the outbox (Phase 8).
- Existing tenants' Receptionist roles don't get the billing permissions automatically.
