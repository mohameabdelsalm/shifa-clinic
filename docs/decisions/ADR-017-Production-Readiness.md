# ADR-017 — Production readiness (Phase 10)

- **Status:** Accepted
- **Date:** 2026-11-18
- **Decided by:** product owner, 2026-11-18:
  - no OpenTelemetry;
  - deploy as a Docker image (Linux);
  - "start and finish the rest" with the recommended defaults for open choices: SMS provider later (`Disabled`), no font file downloaded by hand (the image installs one), nothing deleted without approval.

## Context

Phase 10 closes the gaps left for production: configuration that refuses to start unsafe, the HTTP edge (reverse proxy, CORS, headers), deployment, backups, indexes, and an independent review of Billing (opus/high). That review found an online refund that could be paid twice, plus related issues.

## Decision

### Observability
- **No OpenTelemetry** (product owner). Serilog request/application logs and `/health/live`, `/health/ready` are the monitoring surface. This replaces the "OpenTelemetry in Phase 10" line in system-architecture.md.

### Configuration that fails closed
Outside Development the API refuses to start without each of these:
- `Jwt:SigningKey` (32+ bytes, already the case);
- `DataProtection:KeysPath` (stored gateway secrets);
- `Email:SmtpHost`;
- `Sms:Provider` (`Disabled` until a provider is chosen; `Fake` only in Development);
- `Reports:QuestPdfLicense`;
- `Paymob:UseFake` must be false.

**JWT key rotation.** `Jwt:PreviousSigningKeys` keeps tokens signed by the old key valid until they expire (validation accepts several keys; signing uses only the current one).

**SMS disabled.** With SMS disabled, a portal OTP request answers the usual generic message, creates no code and audits `Portal.OtpSkippedSmsDisabled`. Notifications go by e-mail when there is one; otherwise they are Skipped with "SmsDisabled".

### HTTP edge
- **Forwarded headers** (`X-Forwarded-For/Proto`) are trusted only from `ReverseProxy:KnownNetworks`/`KnownProxies`, first in the pipeline, so the rate limits and logs see the real client.
- **CORS** allows only `Cors:AllowedOrigins`; none = no cross-origin access.
- **Security headers:**
  - `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy`;
  - `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` outside Development;
  - `Cache-Control: no-store` on `/api`.
  - HSTS and the HTTPS redirect are unchanged.

### Deployment
- **`Dockerfile`:** multi-stage build. SDK 10.0.403 builds; the runtime is aspnet 10.0.14 (Ubuntu) and runs as the non-root `app` user on port 8080. It installs `fonts-noto-core` (Noto Naskh Arabic for PDFs), `libfontconfig1` and `tzdata`. State lives only in `/data` (key ring, attachments).
- **`deploy/`:**
  - `docker-compose.yml`: the API and Caddy with automatic HTTPS, on a fixed subnet that matches `ReverseProxy:KnownNetworks`;
  - `Caddyfile`;
  - `.env.example`: every setting, no secrets;
  - `README.md`: the runbook for install, update, key rotation, backups/restore drill, monitoring and the go-live checklist.
- **Migrations** run only with `dotnet Shifa.dll --migrate`, on purpose, at deploy time. It prints the pending list, applies every module in dependency order and installs the Hangfire schema. Development still creates Hangfire's tables itself.
- **Database logins.** The API needs no DDL rights: a migration login (`db_owner`) and an app login (read/write/execute) are separate.
- **Collation.** Production uses `Arabic_CI_AS` like development (sorting/search of Arabic names).

### Indexes (review of the statistics, reports and jobs queries)
- **Appointments:**
  - `(TenantId, LocalDate)`, including `BranchId, DoctorId, Status, StartAt, EndAt` (a covering index for counts per day);
  - `(Status, StartAt)` for the no-show job across clinics.
- **Billing:**
  - Invoices `(TenantId, IssuedAt)`;
  - CreditNotes and Refunds `(TenantId, CreationDate)`;
  - OnlinePaymentAttempts `(Status, CreationDate)` for the expiry job.
- **Migrations:** `Scheduling/AddReportingIndexes` and `Billing/AddReportingIndexes`, which only create indexes.

### Billing fixes (independent review)
1. **Critical — online refund paid twice.** Online refunds now go reserve → gateway → settle:
   - **Reserve.** In its own commit, under a lock on the payment: the amount is reserved on the payment, and a `Refund` (status Pending) and the idempotency record are saved.
   - **Gateway.** The call is not cancellable by the client.
   - **Settle.** Succeeded → Completed and the invoice reopens. Refused → Failed and the reservation is released. Unknown (timeout, pending) → stays Pending, visible and never resent.
   - Settling retries concurrent changes with fresh data instead of failing with 409.
   - A retry with the same key always replays the same refund. Statistics count only Completed refunds.
2. **High — webhook replay against another attempt.** The intention's Paymob order id is stored, and a callback must carry it as the signed `order.id` (`merchant_order_id` isn't signed). A NotApplied return is refused if that transaction is recorded as a payment.
3. **High — unknown gateway outcomes.** `IOnlineRefunds` returns Succeeded / Refused / Unknown and never "not refunded" for an unclear answer. A NotApplied return reserves first (`Refunding`), then calls the gateway, then settles; an unknown outcome stays `Refunding`.
4. **Medium.** Credit notes require an `Idempotency-Key`; a retry replays the same note. The shared `BillingIdempotency` is used by payments, refunds and credit notes.
5. **Medium.** A NotApplied return refunds the captured amount (signed `amount_cents`, stored as `CapturedAmount`).
6. **Low:**
   - request hashes write amounts culture-independently;
   - `PaymobCredentials.ToString()` hides the secrets;
   - a voided visit invoice can be replaced (the lookup and the unique index ignore Void);
   - the dashboard money is by branch-local day, like the reports.
- **Migration:** `Billing/HardenRefunds` adds `Refunds.Status` (existing rows Completed), `OnlinePaymentAttempts.GatewayOrderId` and `CapturedAmount`, and changes the visit-invoice index filter.

## Consequences

- **Old attempts.** Online payment attempts created before this change have no Paymob order id, so their callbacks are ignored. Only development data is affected.
- **Pending refunds.** A refund or return left Pending/`Refunding` needs a person to check Paymob. Automatic reconciliation needs merchant API credentials Shifa doesn't store.
- **Accepted low findings, not changed:**
  - Document numbers use the UTC year: documents issued between 00:00 and 02:00 Cairo time on 1 January get the previous year's series.
  - The portal invoice DTO carries the patient and branch ids of the caller's own linked patients.
- **Not verified here.** Docker isn't running on the development machine, so the image build and compose stack are not run; the first real build is part of go-live.
