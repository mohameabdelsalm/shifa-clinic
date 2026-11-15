# Shifa — Multi-tenant Clinic Management SaaS

Multi-tenant monolith (ASP.NET Core .NET 10, SQL Server, EF Core 10), laid out exactly like Trio ERP HRM/Auth (ADR-009).

## Start of every session

1. Read `project/progress.md` (current phase, what's done, next tasks).
2. Read only the docs relevant to the task: `docs/architecture/*.md`, `docs/decisions/ADR-*.md`, `project/assumptions.md`.
3. Don't re-read the whole repository; search for what you need.

## Working with the developer

- Explain in Arabic with English technical terms inline: business problem first, then the design, then the code flow.
- Copy the Trio ERP HRM/Auth layout and naming exactly (`D:\trio\New folder\Trio-ERP-HRM-API`). Before adding a folder, project or layer HRM doesn't have, propose it to the developer first.
- At the start of each phase, give the execution header (phase, model alias, effort, reason, complexity). The developer switches models from the app's model picker.
- Phase done = build green, docs + `project/progress.md` updated, merged to `develop`. Tests are paused for now (the developer will add them back after the project is finished; the last suite is in git history before ADR-009).

## Solution layout (ADR-009, same as HRM)

```
Shifa.slnx
Core/              Core.csproj — Common, Pagination, Resources (LanguageResource), and per module (Clinic, Identity, Patients, Scheduling, Tenancy):
                   Entities/<M>, Enums/<M>, DTO/<M>, Models/<M>, Interfaces/<M>, Profiles/<M> (AutoMapper), Validations/<M>
Infrastructure/    Infrastructure.csproj — Data/ (ModuleDbContext + one DbContext per module), Configurations/<M>, Migrations/<M>,
                   Repositories/<M>, Services/<M>, Auth/, Common/
Shifa/             Shifa.csproj (the API, like HRM/) — Program.cs (all DI registrations), Controllers/<M>, Middleware/, Extensions/
```

One class per file. Namespaces follow folders without a `Shifa.` prefix: `Core.Entities.Scheduling`, `Infrastructure.Repositories.Patients`, `Shifa.Controllers.Clinic`.

## Multi-tenancy rules (ADR-002, docs/architecture/tenancy-design.md) — security critical

- Tenant ID comes only from the validated token via `ITenantContext`. Never accept `TenantId` from a request body, query string or header. Models (`Add<Name>Model` etc.) have no `TenantId`.
- Tenant-owned entities inherit `TenantOwnedEntity`; global entities are `[PlatformOwned]`. Every entity is one or the other.
- FKs between tenant-owned entities are composite `(TenantId, XId)` using `HasPrincipalKey(p => new { p.TenantId, p.Id })`.
- Business-unique values are unique per tenant: index `(TenantId, Code)`.
- Never use `IgnoreQueryFilters` except in platform repos, with the filter name and an audit record.
- Background jobs and event consumers receive `TenantId` explicitly and set the context before touching data.
- Cache keys start with `t:{tenantId}:`; file paths start with `tenants/{tenantId}/`.
- Every read by ID also checks branch scope and, for patient data, patient access. Out-of-scope → 404.

## Authorization rules (ADR-007)

- Endpoints require `[Permission(Permissions.X)]` (tenant data) or `[PlatformAdmin]` (`api/platform/...`). Anonymous endpoints say `[AllowAnonymous]` explicitly.
- Permissions live in `Core.Common.Permissions`. Adding a feature = add its codes there (and to the default roles).
- Rate-limit new anonymous or SMS/OTP-sending endpoints with `[EnableRateLimiting("auth")]`.
- A repo that hands out roles, permissions or branches must check the caller holds them (`IAccessContext`) — see `StaffRepo`/`RoleRepo`.
- Write an audit row (`IAuditService`) for security-relevant changes and for every Platform Admin action.
- Another tenant's id returns 404, never 403.
## Data and module rules (ADR-009)

- `Core` → nothing of ours. `Infrastructure` → Core. `Shifa` → Infrastructure.
- One `DbContext` per module in `Infrastructure/Data` (inherits `ModuleDbContext`), with its own schema and migrations (`Infrastructure/Migrations/<M>`). Each context applies only `Infrastructure.Configurations.<M>` (its `Module` property). No FKs or joins across schemas.
- A repo uses only its own module's DbContext. To read another module's data, call its `I<Module>Api` (`Core/Interfaces/<M>`), e.g. Scheduling → `IClinicApi`. No compiler check enforces this any more, so review it.
- New migration: `dotnet ef migrations add <Name> -p Infrastructure -s Shifa -c <M>DbContext -o Migrations/<M>`.

## Feature flow (HRM)

- New feature = `Core/Entities/<M>/<Name>.cs`, `Core/Models/<M>/Add<Name>Model.cs` …, `Core/DTO/<M>/<Name>Dto.cs`, `Core/Interfaces/<M>/I<Name>Repo.cs`, `Core/Profiles/<M>/<Name>Profile.cs`, `Core/Validations/<M>/Add<Name>Validator.cs`, `Infrastructure/Configurations/<M>/<Name>Configuration.cs`, `Infrastructure/Repositories/<M>/<Name>Repo.cs`, `Shifa/Controllers/<M>/<Name>Controller.cs`, and its `AddScoped` line in `Program.cs`.
- Names: `Add<Name>Model`, `Update<Name>Model`, `Search<Name>sModel` (input); `<Name>Dto`, `Search<Name>sDto` (output); `I<Name>Repo`/`<Name>Repo`; `<Name>Controller`; `<Name>Configuration`; `<Name>Profile`; `Add<Name>Validator`; enums `<Name>Enum`.
- Flow: `Controller → I<Name>Repo → <Name>Repo → Response`. Controllers stay thin.
- Mapping with AutoMapper (14, MIT) like HRM: `_mapper.Map<Entity>(model)` for add, `_mapper.Map(model, entity)` for update (profiles ignore `Id` and trim/normalize input), `ProjectTo<Dto>(_mapper.ConfigurationProvider)` for reads. DTOs built from several tables (no navigation properties) map the entity part with the mapper and fill the rest in the repo. Never map `TenantId`. No MediatR or generic repository.

## Responses, errors, validation

- Every endpoint returns `Core.Common.Response` (HRM shape). HTTP status = `Response.Status`.
- Messages come from `LanguageResource` (en, ar) or module resources — never hard-coded.
- Business errors: `throw new BusinessException(message, status)`. Unexpected errors: 500 with a generic message; details in logs only.
- FluentValidation validators run automatically for action arguments; the first error is returned as a 400 `Response`.

## Conventions

- Package versions only in `Directory.Packages.props`; check licenses before adding packages.
- Routes `api/<resource>`; platform endpoints `api/platform/<resource>`.
- IDs `long` via HiLo (ADR-003). Money `decimal(18,2)`. Time: inject `TimeProvider`; store UTC `datetimeoffset`.
- `rowversion` on concurrently edited entities. Soft delete via `IsDeleted`; medical and financial records are never hard-deleted.
- Running numbers ("max + 1": MRN, queue number): take `Database.AcquireAsync(...)` (`sp_getapplock`) inside a transaction before reading the max; keep the unique index as the final guard. A retry loop alone fails under bursts.
- Use `CancellationToken` on I/O. Async EF calls.
- Never log medical data, national IDs, passwords, OTPs or tokens.
- No destructive database operations (drop/recreate) without explicit approval. Migrations are applied explicitly, never at app startup.

## Booking rules (ADR-008)

- The final guard against double booking is the unique key on `scheduling.AppointmentSlots` (5-minute ticks per doctor, any branch). Never replace it with code checks, locks or Redis; map its violation to 409.
- Appointment + ticks + history + idempotency record are saved in one `SaveChanges`. Rescheduling: release old ticks, then claim new ones, in one explicit transaction.
- Availability is computed by `AvailabilityCalculator` from schedules (branch-local, IANA time zone), time off and existing appointments; booking re-validates against it.
- Booking endpoints require an `Idempotency-Key` header, scoped to the caller (staff membership or portal phone). Any "taken" outcome (slot, day, unique violation) first checks for a replay of the same key.
- Status changes go through `Appointment.MoveTo` and write an `AppointmentStatusHistory` row and an audit entry. Cancel and no-show release the ticks in the same save. Any change that must not race (reschedule) must UPDATE the appointment row so its `rowversion` is checked. Staff appointment endpoints follow branch scope (outside it = 404).
- The portal gets `PortalAppointmentDto`, never the staff `AppointmentDto`.

## Clinical rules (ADR-010)

- Only the appointment's doctor (the `Doctor` linked to the signed-in membership via `Doctor.MembershipId`) opens, writes and completes a visit; vital signs may be recorded by any `Clinical.Vitals` holder in the visit's branch.
- A completed visit never changes. Corrections are `ClinicalAddendum` rows. Every content write also updates the visit row (`SaveVisitChangeAsync`) so a write racing "complete" fails on the rowversion.
- Visit data follows branch scope (outside = 404). Allergies and conditions are tenant-wide for `Clinical.Read`. Every read of clinical content (staff and portal) writes an audit row, with `CancellationToken.None` so a disconnect can't drop it.
- A doctor's account link and active status can't change while the doctor has open visits (`IClinicalApi.HasOpenVisitsAsync`).
- Files go through `IFileStorage` with server-built paths `tenants/{tenantId}/...`; check type, size and file signature.

## Billing rules (ADR-011)

- Prices come only from the branch price list (snapshot at check-in or invoice creation); no price typing, no discounts for now.
- Invoices are never edited after creation; corrections are credit notes; void only when nothing was paid or credited.
- Every money change (payment, refund, credit, void) goes through the `Invoice` entity methods in the same save, guarded by the invoice rowversion. Invariant: `PaidAmount + CreditedAmount ≤ Total`, never negative.
- Payments and refunds need an `Idempotency-Key` (scoped to the caller) and the caller's open cashier shift at the invoice's branch. Card and Wallet need a reference.
- Document numbers (`INV-`, `RCT-`, `RFD-`, `CN-`) use `DatabaseLocks` + a unique index.

## Patient portal rules (ADR-007)

- Portal endpoints (`api/portal/...`) use `[PortalUser]`; the phone number comes from `IAccessContext.PortalPhoneNumber` (the token), never from the request.
- Anything a portal user can reach must be filtered by `PatientAccountAccess` for that phone; another clinic's or unlinked patient is 404.
- Portal DTOs are separate from staff DTOs and never carry identifiers, national IDs or internal counters.
- OTP-style endpoints never reveal whether a phone number or clinic exists (same answer, same 401), and are rate limited. Never log codes or phone numbers.

## Local environment (ADR-006)

- SQL Server `.` (Windows auth): `Server=.;Database=Shifa;Trusted_Connection=True;TrustServerCertificate=True`.
- `GET /dev/sms` (Development only) shows the SMS the fake sender "sent", e.g. patient OTP codes.
- `docker compose up -d`: Redis, Seq (`localhost:5341`), Mailpit (`localhost:8025`), Azurite.
- `dotnet build Shifa.slnx` before every commit; run the app (`dotnet run --project Shifa`) and try the changed endpoints in Swagger.

## Git

- `main` = releases, `develop` = integration, `feature/<name>` from `develop`. Conventional Commits.
