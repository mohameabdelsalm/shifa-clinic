# Shifa — Multi-tenant Clinic Management SaaS

**Shifa** (شفاء) is a cloud platform that lets many clinics and medical centers run on one system. Each clinic gets its own isolated space for:

- branches, doctors, services and prices;
- staff and permissions;
- patients and appointments, and the patient portal;
- medical records;
- billing and online payments;
- notifications and reports.

Built with **ASP.NET Core (.NET 10)** and **SQL Server** as a **modular monolith**: 8 business modules, each with its own database schema, ~135 REST endpoints, Arabic and English.

> Status: Phases 0–10 of the [roadmap](project/implementation-roadmap.md) implemented. Architecture and every decision are documented in [17 ADRs](docs/decisions/).

---

## What the system offers

### 🏥 For the platform owner (SaaS)
- **Clinics.** Onboard clinics (tenants), each with its first admin and default roles.
- **Plans.** Subscription plans with **limits** (employees, branches, doctors) and **features** switched on or off per plan.
- **Feature overrides.** Per-clinic overrides without code changes: clinical records, billing, patient portal, dashboard, online payments, notifications, reports.
- **Subscriptions.** Trial → active, manual subscription payments, suspend/activate. When a subscription ends the clinic becomes **read-only for 30 days**, then sign-in stops.

### 👩‍⚕️ For clinic staff
- **Clinic setup.** Branches (each with its time zone), specialties, doctors, weekly schedules, time off, services, price per branch.
- **Staff & security.**
  - One account for many clinics, with custom roles from 27 granular permissions.
  - **Branch scope**: staff limited to their branches see and manage only those.
  - Invitations by e-mail; forgot, reset and change password.
- **Patients.** Registration with MRN, identifiers, guardians, consents, and portal access for a family (one phone → several patients), with search.
- **Scheduling.**
  - Real-time availability.
  - **Double-booking impossible** (enforced by the database under concurrency).
  - Idempotent booking; reschedule and cancel.
  - Check-in with queue numbers.
  - Automatic **no-show** after 30 minutes; patients with repeated no-shows need confirmation.
- **Medical records (EMR).** Visits, clinical notes, vital signs, diagnoses, prescriptions, allergies, chronic conditions and attachments. Signed records are immutable (corrections are addenda), and every access is audited.
- **Billing.**
  - The visit invoice is issued at check-in from the price list.
  - Extra invoices; cash, card and wallet payments; partial payments; refunds, credit notes and voids.
  - **Cashier shifts** with expected cash and variance.
  - Every money operation is idempotent and can never overpay or refund twice.
- **Online payments with Paymob.**
  - Each clinic uses its own merchant account; keys are stored encrypted.
  - Patients pay from the portal.
  - Callbacks are verified with HMAC.
  - Refunds go back through the gateway with a safe reserve → call → settle flow.
- **Notifications.** Booking confirmation, reminders **24 h and 2 h** before the visit, reschedule and cancellation messages, payment receipts, by SMS or e-mail. Templates are editable per clinic in Arabic and English.
- **Dashboard & reports.**
  - KPIs against the same period last year, timelines, per-branch and per-doctor breakdowns, payments by method.
  - Reports: daily branch report, revenue vs collections, and doctor performance with **utilization %**.
  - Exports to **Excel** and **PDF (Arabic right-to-left)**.

### 🧑‍🤝‍🧑 For patients (portal)
- Sign in with a **phone one-time code** (no password).
- See available times and book or cancel appointments for themselves and their family.
- View prescriptions and invoices, and **pay online**.

---

## Engineering highlights

| Area | How |
|---|---|
| **Multi-tenancy** | Shared database, strict isolation: EF Core global query filters, composite `(TenantId, Id)` foreign keys, and a save interceptor that rejects cross-tenant writes. The tenant comes only from the signed token, never from the request. |
| **Modular monolith** | 8 modules (Tenancy, Identity, Clinic, Patients, Scheduling, Clinical, Billing, Notifications), each with its own `DbContext`, schema and migrations; modules talk through `I<Module>Api` contracts and integration events. |
| **Reliable events** | Transactional **outbox** (saved with the business change) + dispatcher with leases and backoff, poison handling, and an **inbox** for idempotent consumers. |
| **Concurrency** | `rowversion` optimistic locking, SQL Server application locks for numbering and queues, unique indexes as the last line of defense, Idempotency-Key on every money operation. |
| **Security** | JWT with rotating refresh tokens and reuse detection, account lockout, generic auth errors, rate limiting, audit log, secrets encrypted with Data Protection, security headers, CORS allow-list, configuration that **refuses to start unsafe** in production. Logs never contain medical data, passwords, OTPs or tokens. |
| **Background jobs** | Hangfire: automatic no-show, notification sending, online payment expiry, cleanup. |
| **Time zones** | Appointments, reports and money are per branch-local day (DST-safe). |
| **Localization** | Every message in Arabic and English (`Accept-Language`). |

## Tech stack

**Backend:** .NET 10 · ASP.NET Core Web API · EF Core 10 · SQL Server · AutoMapper · FluentValidation
**Auth:** JWT bearer · refresh-token rotation · ASP.NET Data Protection
**Jobs & messaging:** Hangfire · transactional outbox/inbox
**Integrations:** Paymob (payments) · MailKit (SMTP e-mail) · pluggable SMS sender
**Reports:** QuestPDF (PDF, Arabic RTL) · ClosedXML (Excel)
**Logging:** Serilog (+ Seq locally) · health checks
**Testing:** xUnit v3 · WebApplicationFactory · Respawn · real SQL Server
**Deployment:** Docker (Linux, non-root) · Caddy (automatic HTTPS) · docker compose

## Project structure

```
Core/             Entities, DTOs, Models, Enums, Interfaces, Events, AutoMapper profiles, validators (split by module)
Infrastructure/   DbContexts, EF configurations, migrations, repositories, services (Paymob, notifications, jobs, reports…)
Shifa/            the API: Program.cs, controllers, middleware, extensions
Shifa.Tests/      unit, architecture and integration tests
deploy/           docker compose, Caddy, .env example and the operations runbook
docs/             vision, architecture (ERD, tenancy, security) and ADR-001..017
project/          roadmap, progress and assumptions
```

## Getting started (local)

Prerequisites: .NET 10 SDK and SQL Server on the local default instance (`.`, Windows authentication). Docker is optional (Seq, Mailpit).

1. Build and create/upgrade the database. Every module, in order:
   ```bash
   dotnet build Shifa.slnx
   ```
   ```bash
   dotnet run --project Shifa -- --migrate
   ```
2. Create the first Platform Admin (the password comes from user-secrets, never a file):
   ```bash
   dotnet user-secrets --project Shifa set "PlatformAdmin:Email" "you@example.com"
   ```
   ```bash
   dotnet user-secrets --project Shifa set "PlatformAdmin:Password" "<a strong password>"
   ```
   ```bash
   dotnet run --project Shifa -- --seed-platform-admin
   ```
3. Run the API. It serves Swagger at `/swagger` and health at `/health/live` and `/health/ready`:
   ```bash
   dotnet run --project Shifa
   ```
4. Run the tests:
   ```bash
   dotnet test
   ```

In Development nothing is really sent. Read the SMS (portal codes) at `GET /dev/sms` and the e-mails at `GET /dev/email`; run a background job at once with `POST /dev/jobs/{name}`.

Production deployment (Docker + HTTPS, backups, go-live checklist): **[deploy/README.md](deploy/README.md)**.

## Documentation

| Where | What |
|---|---|
| [docs/vision.md](docs/vision.md) | Product, users, scope, business rules |
| [docs/architecture/](docs/architecture/) | System, module boundaries, tenancy, database (ERD), security |
| [docs/decisions/](docs/decisions/) | ADR-001..017: every architectural decision and why |
| [project/progress.md](project/progress.md) | What was built in each phase and how it was verified |
| [deploy/README.md](deploy/README.md) | Operations runbook |
| [CLAUDE.md](CLAUDE.md) | Rules and conventions for contributors |

---

## بالعربي

**شفاء** منصة سحابية لإدارة العيادات والمراكز الطبية، تخدم عيادات كثيرة على نظام واحد مع عزل كامل لبيانات كل عيادة. تشمل:

- **الإعداد والصلاحيات:** إعداد الفروع والأطباء والخدمات والأسعار، والموظفين والصلاحيات حسب الفرع.
- **المرضى والحجوزات:** ملفات المرضى، والحجز مع منع الحجز المزدوج، وتسجيل الحضور والطابور.
- **الملف الطبي:** الكشف والروشتات والتشخيصات.
- **الفواتير:** الفواتير والتحصيل وورديات الكاشير، والدفع أونلاين عن طريق Paymob.
- **التذكيرات:** تذكيرات بالرسائل والإيميل.
- **بوابة المريض:** دخول برقم الموبايل.
- **التقارير:** تقارير Excel وPDF بالعربي.

مبني بـ .NET 10 وSQL Server، وجاهز للنشر بـ Docker.
