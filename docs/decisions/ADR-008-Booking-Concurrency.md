# ADR-008 — Booking model and double-booking prevention

- **Status:** Accepted
- **Date:** 2026-11-08
- **Scope:** Phase 5 (Scheduling & Booking)

## Context

Reception, phone staff and the patient portal book the same doctors at the same time. A doctor works in several branches with different slot lengths, services have different durations, and the business rule is absolute: **a doctor never has two overlapping appointments, at any branch** (assumption A11). Checking "is it free?" in code and then inserting is a race: two requests both see the slot free and both insert. Redis or in-memory locks don't survive multiple instances or restarts and are not allowed as the final authority (master prompt). Schedules are in the branch's local time; Egypt uses DST, so local→UTC conversion is not a constant offset.

## Decision

### 1. The database decides: 5-minute ticks with a unique key

- Every booked appointment writes one `AppointmentSlot` row **per 5-minute tick** it covers (a 30-minute visit = 6 rows), on a global grid aligned to whole 5 minutes in UTC.
- Unique filtered index `(TenantId, DoctorId, SlotStart) WHERE IsDeleted = 0`.
- Two requests that overlap in even one tick can't both commit: the second gets a unique-key violation, which the API turns into **409 SlotTaken**. This holds across branches (the key has no branch), across slot lengths (the grid is finer than any slot) and across app instances.
- Appointment + its ticks + status history + idempotency record are written in **one `SaveChanges`** (one transaction). Read-committed isolation is enough; no table locks, no serializable transactions.
- Cancelling soft-deletes the ticks (the filter frees them). Rescheduling releases old ticks and claims new ones in one explicit transaction with two saves (release first, then claim), so moving a visit by 15 minutes onto its own old ticks works.
- Consequence for Clinic: schedule start/end times and `SlotMinutes` must be multiples of 5 minutes (validator change in Clinic). Service durations are rounded **up** to whole slots of the doctor's schedule at that branch.

Why ticks and not "check overlaps under a lock": the unique index makes the guarantee declarative and testable; every other approach (application locks, `UPDLOCK/HOLDLOCK` range queries, serializable transactions) is easier to get subtly wrong and harder to prove. Cost: ~6 small rows per appointment (≈2,400/day for a 400-visit tenant), negligible.

### 2. Availability is computed, never trusted

`AvailabilityCalculator` (pure function, unit-tested) builds the free start times for one doctor, branch, service and local date:

1. Take the doctor's `DoctorSchedule` windows at that branch for the date's day of week.
2. Convert each window to UTC with the branch's IANA time zone (`TimeZoneInfo`). Local times that don't exist (spring-forward gap) are skipped; ambiguous ones (fall-back) use the first occurrence.
3. Cut each window into slots of `SlotMinutes`; a start is offered only if `ceil(duration / SlotMinutes)` consecutive slots fit **inside the same window**.
4. Remove starts overlapping the doctor's `DoctorTimeOff`, the doctor's existing active appointments **at any branch**, and starts earlier than now (or later than the booking horizon).

Availability is a hint for the UI (and may be cached later). Booking re-validates everything and the unique index has the last word, so a stale availability list can only produce a 409, never a double booking.

### 3. Booking validation (in order)

Branch active and in the caller's scope · doctor active and works at the branch · service active and its specialty is one of the doctor's · patient exists (`IPatientsApi`) · portal callers: the patient is linked to their phone with `CanBook` · start time is one of the computed available starts (so it is on the schedule grid, inside one window, not in time off, not in the past, within the horizon) · the patient has no other non-cancelled appointment with that doctor on that local day (rule 3, also enforced by a filtered unique index on `(TenantId, PatientId, DoctorId, LocalDate) WHERE Status <> 'Cancelled' AND IsDeleted = 0`) · status: `Pending` if booked online by a patient with ≥ 3 no-shows (rule 5), otherwise `Confirmed`.

### 4. Idempotency

- `POST` booking requires an `Idempotency-Key` header (staff and portal). `IdempotencyRecord` has a unique key `(TenantId, Operation, Key)` and stores a SHA-256 of the canonical request and the resulting appointment id, written in the same transaction as the booking; records expire after 24 hours.
- Same key + same request → the original result (200, same appointment id), no second booking. Same key + different request → **422 IdempotencyKeyReused**. A failed booking stores nothing, so a retry is evaluated again.
- Status changes (confirm, check-in, cancel, no-show) are naturally idempotent: repeating the current state is a no-op 200; an impossible transition is 400.

### 5. Lifecycle and concurrency of changes

- States and transitions as in `docs/vision.md`; methods on the `Appointment` entity enforce them and add a status-history row.
- `Appointment.RowVersion` (`rowversion`): concurrent changes to the same appointment (check-in vs cancel) → the loser gets **409**.
- Check-in: allowed from 2 hours before the start until the end of the appointment's local day; assigns a queue number per (branch, doctor, local date) as `max + 1`, guarded by a unique index with retry (same pattern as MRNs).
- Cancel: staff any time before check-in; portal users only until 3 hours before the start (assumption A9).
- No-show: allowed 30 minutes after the start if not checked in (assumption A10). Phase 5 exposes the transition; the scheduled job that applies it comes in Phase 8.
- **No-show counts are owned by Scheduling** (count of `NoShow` appointments per patient). `Patient.NoShowCount` in Patients becomes a read projection filled by events in Phase 8 and is not the source of truth.

### 6. Module boundaries

- Scheduling stores `BranchId`, `DoctorId`, `ServiceId`, `PatientId` as plain ids (no cross-schema FKs).
- New `IClinicApi` queries: booking inputs for (branch, doctor, service) — branch time zone and status, doctor-at-branch and specialties, service duration and specialty, schedule windows — and the doctor's time off in a UTC range. New `IPatientsApi` use: `PatientExistsAsync`, `GetPortalPatientsAsync`.
- Integration events (`AppointmentBooked`, `AppointmentCancelled`, `PatientCheckedIn`, `AppointmentNoShow`) are defined in `Shifa.Scheduling.Contracts` now and published through the outbox in Phase 8.

### 7. Authorization

- Staff: `Appointments.Read / Create / Cancel / CheckIn`; every appointment belongs to a branch, so branch scope applies to lists, views and changes (out of scope → 404).
- Portal: availability, book, list and cancel only for patients linked to the token's phone (`CanBook` for booking and cancelling).

## Consequences

- Double booking is impossible by construction and provable with a concurrency test against SQL Server.
- Clinic schedules must use 5-minute boundaries; existing data is validated by the new rule on the next save.
- Business settings (cancel window 3 h, no-show 30 min, horizon 90 days, check-in from 2 h before) are constants for now and move to tenant settings later (assumptions A9, A10, A19).

## Alternatives considered

- **One row per scheduled slot** (unique on doctor + slot start): breaks when the same doctor has different slot lengths at two branches or a service spans several slots of different sizes. Rejected.
- **Overlap query under `UPDLOCK, HOLDLOCK` / serializable:** correct only if every writer uses the same query shape; deadlock-prone and hard to test. Rejected.
- **Redis distributed lock:** not durable, not a database guarantee; forbidden as the final authority. Rejected.
- **PostgreSQL exclusion constraint:** not available on SQL Server (ADR-003).
