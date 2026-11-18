# ADR-015 — Outbox implementation, background jobs and patient notifications (Phase 8)

- **Status:** Accepted
- **Date:** 2026-11-18
- **Builds on:** ADR-005 (transactional outbox), ADR-006 (local services), A10 (no-show rule), vision (SMS reminders 24 h and 2 h before)

## Context

Phase 8 needs the outbox decided in ADR-005, scheduled work (no-shows, reminders, housekeeping) and messages to patients: booking confirmations, reminders, changes, cancellations and receipts. No SMS provider has been chosen yet.

## Decision

### Outbox and inbox

- **Opt-in per module.** A module DbContext with `HasOutbox` gets `<schema>.OutboxMessages`; one with `HasInbox` gets `<schema>.InboxMessages`. Now: Scheduling and Billing publish, Notifications consumes. Other modules get no unused tables.
- **Publishing.** `ModuleDbContext.AddEvent(event, now)` adds the message to the same `SaveChanges` as the business change, with the current tenant. If the save fails, no message exists.
  - Scheduling events: `AppointmentBooked`, `AppointmentConfirmed`, `AppointmentRescheduled`, `AppointmentCancelled`, `PatientCheckedIn`, `AppointmentNoShow`.
  - Billing events: `PaymentRecorded`, from desk and online payments.
  - Events carry ids, times and amounts only. Their classes live in `Core/Events/<Module>`, because the HRM layout has no Contracts projects.
  - The class name is the message type, so an event class is never renamed.
- **Dispatching.** `OutboxDispatcher` is a `BackgroundService` that polls every 2 s.
  - It claims a batch with `UPDLOCK, READPAST` and a 2-minute lease, so several API instances never handle the same message at once.
  - Each handler runs in its own scope with the message's tenant.
  - Failures retry with exponential backoff (capped at 1 h).
  - After 10 attempts, or at once for an unknown type, the message is **poisoned**: it is kept, logged and never retried automatically.
  - Logs carry the message id and type, never the payload.
- **Idempotent consumers.** Handlers derive from `InboxEventHandler`. In one transaction it skips a message it already handled, runs the handler and records `(MessageId, Consumer)`.
- **Direct calls kept.** Check-in still calls Billing directly: the check-in response must return the new invoice id, and the call is idempotent and repairable (ADR-011). Visit open and complete also stay direct (ADR-010). Neither needs to wait for an event.

### Jobs (Hangfire)

- Hangfire 1.8 (LGPL-3.0, used unmodified) with SQL Server storage in the `hangfire` schema of the same database. The server runs in the API process; `Jobs:Enabled=false` turns it off on an instance.
- Recurring jobs. Each runs with `DisableConcurrentExecution`, without Hangfire retries (the next run catches up), and handles each clinic in its own tenant scope.

| Job | When | What |
|---|---|---|
| `scheduling-no-show` | every 5 min | Confirmed and not checked in 30 min after the start → NoShow (A10). Releases the doctor's time and publishes `AppointmentNoShow`. A concurrent check-in wins (rowversion). |
| `notifications-send` | every minute | Sends due notifications. |
| `online-payments-expire` | every 30 min | Online attempts still Pending 1 h after the Paymob intention expired → `Expired`. Not final: a late callback is still recorded. |
| `messaging-cleanup` | daily 03:00 UTC | Removes processed outbox and inbox rows older than 14 days. |

- **Not deleted by the cleanup job.** Business data, security tokens and poisoned or unprocessed messages are never deleted. Their retention needs a product/legal decision.
- **Development helpers.** The dashboard is at `/hangfire`, local requests only. `POST /dev/jobs/{name}` runs a job at once.

### Notifications

- **Module.** `notifications` schema with three tables:
  - `Notifications`: kind, channel, recipient, language, payload, status, scheduled time, attempts.
  - `DeliveryAttempts`.
  - `NotificationTemplates`: the clinic's own texts.
- **Feature.** `Feature.Notifications`, on by default; when off, nothing is planned or sent.
- **Permissions.** `Notifications.Read` (BranchManager and Receptionist) and `Notifications.Manage` (TenantAdmin).
- **Planning** (event handlers):
  - A confirmed booking → confirmation now, plus reminders 24 h and 2 h before the start. A reminder that would go out within 10 minutes is not planned.
  - A Pending booking waits for `AppointmentConfirmed`.
  - Rescheduled → unsent messages cancelled, then a "moved" message and new reminders.
  - Cancelled → unsent messages cancelled, then a cancellation message.
  - Checked in or no-show → unsent messages cancelled.
  - Payment → receipt.
- **Recipient.** The patient's phone, else a linked portal phone (the patient's own link first, then a guardian), as SMS. Without a phone, the e-mail. Without either, nothing.
- **Content.** Templates use named placeholders only:
  - Appointment messages: `{patient}` `{clinic}` `{doctor}` `{branch}` `{date}` `{time}`.
  - Receipts: `{patient}` `{clinic}` `{amount}` `{currency}` `{invoice}` `{receipt}`.
  - Nothing medical is ever offered.
  - Defaults are in the resources (ar and en). A clinic can override per kind, channel and language, and unknown placeholders are refused.
  - The language is `Notifications:Language` (default `ar`); there is no per-patient language yet.
- **Sending.** Before an appointment message, the sender checks the appointment is still Confirmed at the planned start and still ahead. Otherwise the message is cancelled or skipped. This makes the result independent of event order.
  - Messages are skipped if the clinic is inactive or the feature is off.
  - A provider failure retries after 5 minutes; after 3 attempts the message is Failed.
  - Each try is a `DeliveryAttempt` that records the exception type only.
- **Staff API.** `POST api/notifications/search` (HRM style, branch scope, masked recipient, no text), and `GET/PUT api/notification-templates`.
- **Security messages bypass this module.** OTP codes and password links are still sent directly: they must go at once and must never be stored.

### E-mail

- `SmtpEmailSender` uses MailKit instead of `System.Net.Mail.SmtpClient`, which Microsoft no longer recommends. `Email:SmtpSecurity` is None, StartTls, SslOnConnect or Auto.
- Mailpit stays the local SMTP catcher (ADR-006). Docker images are now pinned to versions.

## Consequences

- **SMS provider still open.** `ISmsSender` is the fake; choosing a provider is a configuration and implementation step (Phase 10). Until then reminders are visible only in `/dev/sms` locally.
- **Delayed side effects.** Side effects of an event happen about 2 seconds after the change. Reminders are planned when the appointment is booked, so a changed doctor or branch name shows only in messages planned later.
- **Hangfire schema.** Hangfire creates its own schema on first start (`PrepareSchemaIfNecessary`). The dashboard is not exposed outside Development; production monitoring is part of Phase 10.
- **Reconciliation not built.** Real reconciliation with Paymob (transaction inquiry) needs merchant API credentials Shifa doesn't store.
