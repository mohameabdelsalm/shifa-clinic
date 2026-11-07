# ADR-010 — Clinical records (EMR)

- **Status:** Accepted
- **Date:** 2026-11-07
- **Decided by:** product owner, 2026-11-07 (read scope, vitals, attachments)

## Context

Phase 6 adds the medical record: visits, clinical notes, vital signs, ICD-10 diagnoses, prescriptions, attachments, addenda, and patient-level allergies and chronic conditions (ERD in `docs/architecture/database-design.md`, business rule 7 in `docs/vision.md`). Medical data is the most sensitive data in the system: it must not change silently, every read must be traceable, and staff may only reach what their role and branches allow.

## Decision

**Module and data.** A `clinical` schema with its own `ClinicalDbContext` (ADR-009 layout): `Visits`, `ClinicalNotes`, `VitalSigns`, `Diagnoses`, `Prescriptions`, `PrescriptionItems`, `VisitAttachments`, `ClinicalAddenda`, `PatientAllergies`, `PatientConditions`. Child rows use composite tenant FKs to `Visits`. One visit per appointment (unique `(TenantId, AppointmentId)`), at most one note and one prescription per visit.

**Who is the visit's doctor.** A staff account is linked to a doctor record: `Doctor.MembershipId` (Clinic, unique per tenant, set with `PUT api/doctors/account` by `Doctors.Manage`). The signed-in member's doctor is found through `IClinicApi`.

**Visit lifecycle (rule 7).**
1. *Open:* only the appointment's doctor, only when the appointment is `CheckedIn`. Clinical first asks Scheduling (`ISchedulingApi.StartVisitAsync`) to move the appointment `CheckedIn → InProgress` (rowversion-protected), then inserts the visit. Opening again returns the same visit; if a crash left the appointment `InProgress` without a visit, opening again creates it.
2. *Edit while Open:* note, diagnoses (one primary at most), prescription and attachments — only the visit's doctor. Vital signs — anyone with the new permission `Clinical.Vitals` (Doctor and Nurse roles) in the visit's branch.
3. *Complete:* only the visit's doctor. The visit becomes `Completed` (read-only for ever), then Scheduling moves the appointment `InProgress → Completed`. Completing again is a no-op that also retries the Scheduling step.
4. *After completion:* no edits. Corrections are `ClinicalAddendum` rows (author user, time, reason, content) by `Clinical.Write` holders in the visit's branch. Addenda are append-only.

Immutability is enforced in the entity (`Visit.EnsureOpen`) and checked again in every repo write.

**Who reads what (product owner).**
- Visits, notes, diagnoses, prescriptions, attachments: `Clinical.Read` and the visit's branch inside the reader's branch scope. Outside it → 404.
- Allergies and chronic conditions: patient-level, visible to every `Clinical.Read` holder in the tenant, for patient safety. Changing them needs `Clinical.Write`.
- Every read of clinical content (visit detail, patient clinical summary, attachment download) writes an audit row.

**Attachments (product owner: now, on disk).** `IFileStorage` with a local-disk implementation (`FileStorage:RootPath`, default `App_Data/files`). Paths are built by the server only: `tenants/{tenantId}/visits/{visitId}/{guid}.{ext}`. PDF, JPEG and PNG, up to 10 MB; the file signature must match the type. An Azure Blob implementation can replace it later without code changes elsewhere.

**Portal.** `GET api/portal/prescriptions`: prescriptions of completed visits of the patients linked to the signed-in phone, in a portal DTO (no notes, diagnoses or internal fields).

## Consequences

- Opening and completing a visit touch two schemas without one transaction. The order (Scheduling first on open; Clinical first on complete) plus idempotent retries keeps them consistent; Phase 8's outbox can replace the direct calls later.
- Existing tenants' Doctor and Nurse roles don't get `Clinical.Vitals` automatically (default roles apply to new tenants); an admin adds it.
- A doctor record without a linked account can be booked but can't open visits.
- A doctor's account link and active status can't change while the doctor has open visits (review C2/C4), so visits can't be handed to someone else or orphaned.
