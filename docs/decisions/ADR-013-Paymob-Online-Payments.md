# ADR-013 — Online payments with Paymob

- **Status:** Accepted
- **Date:** 2026-11-16
- **Decided by:** product owner, 2026-11-16 (each clinic's own Paymob account; patients pay from the portal; refunds from Shifa through the Paymob API)

## Context

Patients should be able to pay their invoice balance online (cards and Egyptian wallets) without the cash desk. Paymob is the gateway (Egypt). Money must go straight to each clinic, Shifa must never see card data, and a forged or repeated callback must never create or double a payment. Sources: Paymob developer docs (Create Intention `POST /v1/intention/`, Unified Checkout `https://eg.checkout.paymob.com/?publicKey=…&clientSecret=…`, transaction callback HMAC-SHA512, Refund `POST /api/acceptance/void_refund/refund`), read 2026-11-16.

## Decision

- **Feature and settings.** Online payments need `Feature.OnlinePayments` (ADR-012, off by default) and the clinic's gateway settings: public key, secret key, HMAC secret and integration ids. The secrets are stored encrypted with ASP.NET Data Protection and are never returned by the API. Settings are managed by `Billing.Settings` holders (TenantAdmin by default).
- **Paying.** `POST api/portal/invoices/{id}/pay-online` (portal user linked to the invoice's patient): Shifa creates an `OnlinePaymentAttempt` and a Paymob intention for exactly the invoice balance. Details:
  - Amount in piasters; currency = invoice currency; `payment_methods` = the clinic's integration ids.
  - `special_reference` = the attempt's own reference (comes back as `merchant_order_id`).
  - `notification_url` = `{PublicBaseUrl}/api/webhooks/paymob/{tenantId}`; `redirection_url` = the portal's return page.
  - The response gives the Unified Checkout URL; the patient pays on Paymob's page (Shifa never sees card data).
- **Callback = source of truth.** `POST api/webhooks/paymob/{tenantId}` (anonymous, rate-limited):
  1. Runs in that tenant's scope and verifies the `hmac` query parameter. HMAC-SHA512 with the clinic's HMAC secret over the 20 documented fields, in order; constant-time compare. A wrong HMAC → 401 and a warning log.
  2. On a successful, non-pending, non-refunded, non-voided transaction whose amount equals the attempt: records a `Payment` (Method `Online`, `Reference` = Paymob transaction id, no shift) through `Invoice.TryApplyPayment` in one save.
  3. The unique index on (TenantId, Reference) for online payments makes a repeated callback a no-op.
  4. If the invoice was settled at the desk meanwhile (the amount no longer fits), the attempt is marked `NotApplied` and audited for a refund decision; the invariant is never broken.
  5. Failed transactions mark the attempt `Failed`; a later success for the same attempt (another card) is still recorded. Refund callbacks are ignored, since refunds are recorded when made.
  6. Callbacks for one attempt run under an app lock on that attempt, so parallel duplicates can't both record.
- **Returning a NotApplied payment.** `POST api/online-payments/{id}/refund` (`Billing.Refund`) refunds the full amount through Paymob under the same lock and marks the attempt `Refunded`. Staff follow attempts with `POST api/online-payments/search`.
- **Refunds.** `PaymentRepo.Refund` on an online payment calls `IOnlineRefunds` (Paymob refund API, `transaction_id` + `amount_cents`). The refund is recorded only after Paymob confirms success, so a failed refund records nothing.
- **Local development.** With `Paymob:UseFake=true` in the Development environment only, a fake client returns synthetic intentions and refunds, so the whole flow (including signed callbacks) can be tested without merchant keys. The fake is refused outside Development.

## Consequences

- A clinic must create its own Paymob merchant account, enable online payments for it (feature) and enter its keys.
- If Paymob refunds successfully but saving the refund in Shifa then fails (e.g. a concurrent change, 409), money has left without a record. The audit log has the Paymob refund id and the error so it can be reconciled; a reconciliation job against Paymob's transaction inquiry API is a follow-up (Phase 8).
- The Data Protection key ring must be persisted and shared in production (Phase 10), or stored secrets become unreadable after a redeploy.
