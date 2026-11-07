# Database design

Decision: ADR-003. Tenancy rules: [tenancy-design.md](tenancy-design.md).

## Conventions

- One SQL Server database, one schema per module. Tables and columns use EF Core default PascalCase names.
- `Id` is `bigint` from a HiLo sequence in the module schema.
- Every table has the `BaseModel` columns: `CreationDate`, `CreatedBy`, `ModificationDate`, `ModifiedBy`, `IsDeleted`. Omitted from the diagrams.
- **Tenant-owned** tables also have `TenantId` and a unique alternate key `(TenantId, Id)`. Foreign keys between tenant-owned tables are composite `(TenantId, XId)`. Diagrams show `TenantId` once per table and draw the relationship once.
- **Platform-owned** tables (marked ◆ below) have no `TenantId`.
- Solid line = FK inside the schema. Dashed line = reference to another module by ID (no FK).
- Money `decimal(18,2)`; times `datetimeoffset` UTC; `rowversion` where noted (RV); enums stored as `nvarchar(50)` text.
- Each schema has its own HiLo sequence `EntityFrameworkHiLoSequence` and `__EFMigrationsHistory` table.
- The local server collation is `Arabic_CI_AS` (databases inherit it). The production collation is decided in Phase 10.

## Identity (`identity`)

```mermaid
erDiagram
  User ||--o{ Membership : has
  Membership ||--o{ MembershipRole : has
  Role ||--o{ MembershipRole : assigned
  Role ||--o{ RolePermission : grants
  Membership ||--o{ MembershipBranch : "branch scope"
  User ||--o{ RefreshToken : owns
  User {
    bigint Id PK "◆ platform-owned"
    nvarchar Email UK "filtered not null"
    nvarchar PhoneNumber UK "filtered not null"
    nvarchar PasswordHash
    bit IsPlatformAdmin
    nvarchar Status
  }
  Membership {
    bigint TenantId
    bigint UserId "unique with TenantId"
    bit AllBranches
    nvarchar Status "Invited, Active, Suspended"
  }
  Role {
    bigint TenantId
    nvarchar Name "unique per tenant"
    bit IsSystemDefault
  }
  RolePermission {
    bigint TenantId
    bigint RoleId FK
    nvarchar PermissionCode "code-defined catalog"
  }
  MembershipRole {
    bigint TenantId
    bigint MembershipId FK
    bigint RoleId FK
  }
  MembershipBranch {
    bigint TenantId
    bigint MembershipId FK
    bigint BranchId "→ clinic.Branches"
  }
  RefreshToken {
    bigint UserId FK "◆"
    bigint TenantId "token scope"
    nvarchar TokenHash UK
    datetimeoffset ExpiresAt
    datetimeoffset RevokedAt
    bigint ReplacedById
  }
  OtpChallenge {
    bigint Id PK "◆"
    nvarchar PhoneNumber
    nvarchar CodeHash
    nvarchar Purpose
    int Attempts
    datetimeoffset ExpiresAt
  }
```

## Tenancy (`tenancy`)

```mermaid
erDiagram
  Tenant ||--o{ TenantSubscription : has
  Plan ||--o{ PlanEntitlement : defines
  Plan ||--o{ TenantSubscription : "subscribed to"
  TenantSubscription ||--o{ SubscriptionPayment : "paid by"
  Tenant {
    bigint Id PK "◆"
    nvarchar Name
    nvarchar Slug UK
    nvarchar Status "Trial, Active, Suspended, Closed"
    nvarchar DefaultTimeZoneId "Africa/Cairo"
    nvarchar CurrencyCode "EGP"
  }
  Plan {
    bigint Id PK "◆"
    nvarchar Code UK
    nvarchar Name
    decimal Price
    nvarchar BillingPeriod "Monthly, Yearly"
    int TrialDays
    bit IsActive
  }
  PlanEntitlement {
    bigint PlanId FK "◆"
    nvarchar Code "MaxBranches, MaxDoctors, Module.Billing ..."
    nvarchar ValueType "Limit, Feature"
    int LimitValue
    bit Enabled
  }
  TenantSubscription {
    bigint TenantId FK "◆ platform-managed"
    bigint PlanId FK
    nvarchar Status "Trial, Active, PastDue, Suspended, Cancelled, Expired"
    datetimeoffset StartsAt
    datetimeoffset EndsAt
    datetimeoffset TrialEndsAt
  }
  SubscriptionPayment {
    bigint TenantSubscriptionId FK "◆"
    decimal Amount
    nvarchar CurrencyCode
    datetimeoffset PaidAt
    nvarchar Reference "manual; no gateway yet"
  }
```

Usage (branches, doctors, employees) is counted on demand through `IClinicApi` / `IIdentityApi` and compared with `PlanEntitlement` limits when creating those records.

## Clinic setup (`clinic`)

```mermaid
erDiagram
  Branch ||--o{ DoctorBranch : hosts
  Doctor ||--o{ DoctorBranch : "works at"
  DoctorBranch ||--o{ DoctorSchedule : "weekly hours"
  Doctor ||--o{ DoctorTimeOff : takes
  Doctor ||--o{ DoctorSpecialty : has
  Specialty ||--o{ DoctorSpecialty : classifies
  Specialty ||--o{ Service : groups
  Service ||--o{ ServicePrice : "priced per branch"
  Branch ||--o{ ServicePrice : sets
  Branch {
    bigint TenantId
    nvarchar Name "unique per tenant"
    nvarchar Code "unique per tenant"
    nvarchar TimeZoneId
    nvarchar Phone
    nvarchar Address
    nvarchar Status
  }
  Doctor {
    bigint TenantId
    bigint UserId "→ identity.Users, nullable"
    nvarchar FullNameEn
    nvarchar FullNameAr
    nvarchar Title
    nvarchar Status
  }
  Specialty {
    bigint TenantId
    nvarchar NameEn
    nvarchar NameAr
  }
  DoctorSpecialty {
    bigint TenantId
    bigint DoctorId FK
    bigint SpecialtyId FK
  }
  DoctorBranch {
    bigint TenantId
    bigint DoctorId FK
    bigint BranchId FK
    nvarchar Status
  }
  DoctorSchedule {
    bigint TenantId
    bigint DoctorBranchId FK
    int DayOfWeek
    time StartTime
    time EndTime
    int SlotMinutes
  }
  DoctorTimeOff {
    bigint TenantId
    bigint DoctorId FK
    bigint BranchId "null = all branches"
    datetimeoffset FromAt
    datetimeoffset ToAt
    nvarchar Reason
  }
  Service {
    bigint TenantId
    bigint SpecialtyId FK
    nvarchar NameEn
    nvarchar NameAr
    int DurationMinutes
    nvarchar Status
  }
  ServicePrice {
    bigint TenantId
    bigint ServiceId FK
    bigint BranchId FK
    decimal Price
    datetimeoffset EffectiveFrom
    datetimeoffset EffectiveTo
  }
```

## Patients (`patients`)

```mermaid
erDiagram
  Patient ||--o{ PatientIdentifier : has
  Patient ||--o{ PatientGuardian : "cared for by"
  Patient ||--o{ PatientAccountAccess : "accessible by"
  Patient ||--o{ PatientConsent : gives
  Patient {
    bigint TenantId
    nvarchar Mrn "unique per tenant"
    nvarchar FullName
    date DateOfBirth
    nvarchar Gender
    nvarchar PhoneNumber
    nvarchar Email
    nvarchar Address
    int NoShowCount
  }
  PatientIdentifier {
    bigint TenantId
    bigint PatientId FK
    nvarchar Type "NationalId, Passport"
    nvarchar Value "unique per tenant + type"
  }
  PatientGuardian {
    bigint TenantId
    bigint PatientId FK
    nvarchar FullName
    nvarchar Relationship
    nvarchar PhoneNumber
  }
  PatientAccountAccess {
    bigint TenantId
    bigint PatientId FK
    bigint UserId "→ identity.Users"
    nvarchar Relationship "Self, Parent, Guardian"
    bit CanBook
  }
  PatientConsent {
    bigint TenantId
    bigint PatientId FK
    nvarchar ConsentType
    datetimeoffset GrantedAt
    datetimeoffset RevokedAt
  }
```

## Scheduling (`scheduling`)

```mermaid
erDiagram
  Appointment ||--|{ AppointmentSlot : occupies
  Appointment ||--o{ AppointmentStatusHistory : tracks
  Appointment {
    bigint TenantId
    bigint BranchId "→ clinic"
    bigint DoctorId "→ clinic"
    bigint ServiceId "→ clinic"
    bigint PatientId "→ patients"
    datetimeoffset StartAt
    datetimeoffset EndAt
    nvarchar Status
    nvarchar Source "Online, Reception, Phone"
    int QueueNumber
    datetimeoffset CheckedInAt
    rowversion RowVersion "RV"
  }
  AppointmentSlot {
    bigint TenantId
    bigint DoctorId "unique with TenantId + SlotStart"
    datetimeoffset SlotStart
    bigint AppointmentId FK
  }
  AppointmentStatusHistory {
    bigint TenantId
    bigint AppointmentId FK
    nvarchar FromStatus
    nvarchar ToStatus
    nvarchar Reason
  }
  IdempotencyRecord {
    bigint TenantId
    nvarchar Key "unique with TenantId + Operation"
    nvarchar Operation
    nvarchar RequestHash
    nvarchar ResponseJson
    datetimeoffset ExpiresAt
  }
```

Slots are per doctor (not per branch) so a doctor can never be booked in two branches at the same time.

## Clinical (`clinical`)

```mermaid
erDiagram
  Visit ||--o{ ClinicalNote : has
  Visit ||--o{ VitalSign : records
  Visit ||--o{ Diagnosis : has
  Visit ||--o| Prescription : issues
  Prescription ||--o{ PrescriptionItem : contains
  Visit ||--o{ VisitAttachment : has
  Visit ||--o{ ClinicalAddendum : amended_by
  Visit {
    bigint TenantId
    bigint AppointmentId "unique per tenant, → scheduling"
    bigint PatientId "→ patients"
    bigint DoctorId "→ clinic"
    bigint BranchId "→ clinic"
    nvarchar Status "Open, Completed"
    datetimeoffset CompletedAt
    rowversion RowVersion "RV"
  }
  ClinicalNote {
    bigint TenantId
    bigint VisitId FK
    nvarchar ChiefComplaint
    nvarchar Notes
  }
  VitalSign {
    bigint TenantId
    bigint VisitId FK
    int BpSystolic
    int BpDiastolic
    int HeartRate
    decimal TemperatureC
    decimal WeightKg
  }
  Diagnosis {
    bigint TenantId
    bigint VisitId FK
    nvarchar Icd10Code
    bit IsPrimary
  }
  Prescription {
    bigint TenantId
    bigint VisitId FK
  }
  PrescriptionItem {
    bigint TenantId
    bigint PrescriptionId FK
    nvarchar DrugName
    nvarchar Dosage
    nvarchar Frequency
    int DurationDays
  }
  VisitAttachment {
    bigint TenantId
    bigint VisitId FK
    nvarchar StoragePath "tenants/{tenantId}/..."
    nvarchar ContentType
    bigint SizeBytes
  }
  ClinicalAddendum {
    bigint TenantId
    bigint VisitId FK
    bigint AuthorUserId
    nvarchar Reason
    nvarchar Content
  }
  PatientAllergy {
    bigint TenantId
    bigint PatientId "→ patients"
    nvarchar Allergen
    nvarchar Severity
  }
  PatientCondition {
    bigint TenantId
    bigint PatientId "→ patients"
    nvarchar Condition
    date Since
  }
```

A completed visit is read-only; changes are `ClinicalAddendum` rows with author, time and reason.

## Billing (`billing`)

```mermaid
erDiagram
  Invoice ||--o{ InvoiceItem : contains
  Invoice ||--o{ Payment : "paid by"
  Payment ||--o{ Refund : "refunded by"
  Invoice ||--o{ CreditNote : "corrected by"
  CashierShift ||--o{ Payment : collects
  Invoice {
    bigint TenantId
    bigint BranchId "→ clinic"
    bigint PatientId "→ patients"
    bigint AppointmentId "→ scheduling"
    nvarchar Number "unique per tenant"
    nvarchar Status "Draft, Issued, PartiallyPaid, Paid, Void"
    decimal Subtotal
    decimal Discount
    decimal Tax
    decimal Total
    decimal PaidAmount
    rowversion RowVersion "RV"
  }
  InvoiceItem {
    bigint TenantId
    bigint InvoiceId FK
    bigint ServiceId "→ clinic"
    nvarchar Description
    decimal Quantity
    decimal UnitPrice "price snapshot"
    decimal LineTotal
  }
  Payment {
    bigint TenantId
    bigint InvoiceId FK
    bigint CashierShiftId FK
    decimal Amount
    nvarchar Method "Cash, Card, Wallet"
    nvarchar IdempotencyKey "unique per tenant"
  }
  Refund {
    bigint TenantId
    bigint PaymentId FK
    decimal Amount
    nvarchar Reason
  }
  CreditNote {
    bigint TenantId
    bigint InvoiceId FK
    nvarchar Number "unique per tenant"
    decimal Amount
    nvarchar Reason
  }
  CashierShift {
    bigint TenantId
    bigint BranchId "→ clinic"
    bigint CashierUserId
    datetimeoffset OpenedAt
    datetimeoffset ClosedAt
    decimal OpeningCash
    decimal ClosingCash
  }
```

Issued invoices are never edited; corrections are credit notes. Revenue (invoiced) and collections (payments − refunds) are reported separately.

## Notifications (`notifications`) and Reporting (`reporting`)

```mermaid
erDiagram
  Notification ||--o{ DeliveryAttempt : "sent via"
  NotificationTemplate {
    bigint TenantId "null-free: platform defaults copied on onboarding"
    nvarchar Code
    nvarchar Channel "Sms, Email, WhatsApp, InApp"
    nvarchar Language "en, ar"
    nvarchar Body
  }
  Notification {
    bigint TenantId
    bigint RecipientUserId
    nvarchar Channel
    nvarchar TemplateCode
    nvarchar PayloadJson
    nvarchar Status
    datetimeoffset ScheduledAt
  }
  DeliveryAttempt {
    bigint TenantId
    bigint NotificationId FK
    nvarchar Provider
    nvarchar Result
    nvarchar Error
  }
  NotificationPreference {
    bigint TenantId
    bigint UserId
    nvarchar Channel
    bit Enabled
  }
  DailyBranchStat {
    bigint TenantId
    bigint BranchId
    date Day "unique with TenantId + BranchId"
    int Appointments
    int NoShows
    decimal Invoiced
    decimal Collected
  }
```

## Shared tables in every schema

| Table | Purpose |
|---|---|
| `OutboxMessages` | `Id` (Guid), `TenantId`, `Type`, `PayloadJson`, `OccurredAt`, `ProcessedAt`, `Attempts`, `Error` |
| `InboxMessages` | `MessageId` (Guid) + `Consumer` (PK), `ProcessedAt` |

`identity` also holds `AuditLogs` for security and medical-record access events (who, tenant, action, entity, time, IP).

## Key constraints

| Table | Constraint | Why |
|---|---|---|
| All tenant-owned | Unique `(TenantId, Id)`; composite FKs | Cross-tenant references are impossible in the database |
| `scheduling.AppointmentSlots` | Unique `(TenantId, DoctorId, SlotStart)` | Final protection against double booking under concurrency |
| `scheduling.IdempotencyRecords` | Unique `(TenantId, Operation, Key)` | Retried booking requests return the first result |
| `billing.Payments` | Unique `(TenantId, IdempotencyKey)` | No duplicate payments |
| `clinical.Visits` | Unique `(TenantId, AppointmentId)` | One visit per appointment |
| `patients.Patients` | Unique `(TenantId, Mrn)` | MRN unique inside a tenant |
| `identity.Memberships` | Unique `(TenantId, UserId)` | One membership per user per tenant |
| `identity.Users` | Filtered unique `Email`, `PhoneNumber` | Optional but unique when present |
| `*.OutboxMessages` | Filtered index `WHERE ProcessedAt IS NULL` | Fast dispatcher polling |
