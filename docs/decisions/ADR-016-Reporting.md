# ADR-016 — Reporting with Excel and PDF exports (Phase 9)

- **Status:** Accepted
- **Date:** 2026-11-18
- **Decided by:** product owner, 2026-11-18: reports as JSON + Excel + PDF. The license type for QuestPDF is the product owner's to set.

## Context

Clinic owners need the numbers of a period per branch, money billed against money collected, and doctor performance including utilization (vision: "daily report"). They need them on screen and as files to print or share. Trio HRM exports Excel with ClosedXML and has no PDF.

## Decision

- **Computed live, no projection.** Reports read the same module statistics APIs as the dashboard:
  - `ISchedulingApi.CountAppointmentsAsync`, now with minutes;
  - `IBillingApi.GetMoneyFlowsByLocalDayAsync`;
  - `IClinicApi.GetDoctorCapacityAsync`.
  
  The `DailyBranchStat` table from database-design.md is **not built**. Live numbers are always correct and need no backfill or rebuild job. A projection can be added if the Phase 10 performance review shows a need.
- **Shared rules.** `AppointmentStats` holds the shared definitions, and the dashboard and the reports both use it:
  - booked = all − cancelled;
  - no-show rate = no-shows / (came + no-shows);
  - booked minutes = minutes of appointments not cancelled and not no-show.
- **Branch-local days.** Appointments already use the branch's local date. Money in reports is now dated by the branch's time zone too (DST included): a payment at 00:30 Cairo time counts for that Cairo day. The dashboard keeps UTC days until it is changed on purpose.
- **Reports.** `POST api/reports/...` with `ReportModel`: `DateFrom`/`DateTo` (local days, at most 366), `BranchIds` (limited to the caller's scope), `DoctorId` (doctors report only) and `Format`.
  - `daily-branches`: per day and branch, appointments, completed, no-shows, cancelled, no-show rate, revenue (invoiced − credited) and collections (payments − refunds).
  - `revenue`: every day of the period, invoiced, credited, revenue, payments by method, refunds, collections, and "not collected" (revenue − collections).
  - `doctors`: per doctor, the counts, available minutes (schedule minus time off, by branch time zone), booked minutes and utilization %.
  - Each report has a totals row.
- **Formats.**
  - `Json` (default): the usual `Response`.
  - `Xlsx` (ClosedXML, MIT): one sheet. Numbers are real numbers and text is never a formula. The sheet is right-to-left for Arabic, and the header row is frozen.
  - `Pdf` (QuestPDF): A4 landscape, right-to-left for Arabic, header with clinic, title and period, a bold totals row, and a footer with the generation time and page numbers.
  - Titles and headers follow the request language (`Accept-Language`).
- **Access.** `Reports.Read` (TenantAdmin, BranchManager) and `Feature.Reports` (on by default). Reports are reads, so they still work while a clinic is read-only (A6).
- **Audit.** Every file export is audited (`Reports.Exported`: report, format, period); screen views are not (security-design.md).
- **QuestPDF license and fonts.**
  - `Reports:QuestPdfLicense` must be set outside Development: Community is only for organizations under USD 1M annual gross revenue, otherwise Professional or Enterprise. Development defaults to Community.
  - QuestPDF 2026.9+ uses only fonts it can find and fails instead of substituting.
  - `Reports:UseSystemFonts` (default true) allows the machine's fonts. `Reports:PdfFontFamilies` lists the families to use (default on Windows: Arial, Tahoma; elsewhere: Noto Naskh Arabic).
  - Font files in `Shifa/Fonts` are deployed with the app and found automatically.

## Consequences

- **Fonts in production.** Production on Linux needs an Arabic-capable font deployed (`Shifa/Fonts`) or installed, plus a matching `Reports:PdfFontFamilies`. Phase 10 task; downloading a font file needs the product owner's approval.
- **Query cost.** Long periods read appointment groups and money rows live. The 366-day limit bounds it; index review is in Phase 10.
- **Bidi display.** Inside Arabic text, a date like 2026-11-18 is displayed by bidi rules as 18-11-2026 (still correct).
- **Not built.** Revenue per doctor, a scheduled daily e-mail with the PDF to the owner, and custom report builders.
