using System.Globalization;
using System.Net;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.DTO.Reporting;
using Core.Enums.Billing;
using Core.Enums.Reporting;
using Core.Interfaces.Billing;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Interfaces.Reporting;
using Core.Interfaces.Scheduling;
using Core.Interfaces.Tenancy;
using Core.Models.Reporting;
using Infrastructure.Services.Reporting;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Reporting;

/// <summary>
/// Reports (ADR-016), computed live from the modules' statistics APIs like the dashboard (no stored projection), in the
/// caller's branch scope, by branch-local day. JSON for the screen, or an Excel/PDF file; every file export is audited.
/// </summary>
public class ReportRepo : IReportRepo
{
    private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfType = "application/pdf";

    private readonly ISchedulingApi _scheduling;
    private readonly IBillingApi _billing;
    private readonly IClinicApi _clinic;
    private readonly ITenancyApi _tenancy;
    private readonly ITenantContext _tenant;
    private readonly IAccessContext _access;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public ReportRepo(ISchedulingApi scheduling, IBillingApi billing, IClinicApi clinic, ITenancyApi tenancy, ITenantContext tenant, IAccessContext access,
        IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _scheduling = scheduling;
        _billing = billing;
        _clinic = clinic;
        _tenancy = tenancy;
        _tenant = tenant;
        _access = access;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    /// <summary>One row per local day and branch: appointments and money. (DoctorId does not apply: money isn't per doctor.)</summary>
    public async Task<ReportOutput> DailyBranches(ReportModel model, CancellationToken cancellationToken)
    {
        var branches = BranchesInScope(model);
        var counts = await _scheduling.CountAppointmentsAsync(model.DateFrom, model.DateTo, branches, null, cancellationToken);
        var money = await _billing.GetMoneyFlowsByLocalDayAsync(model.DateFrom, model.DateTo, branches, cancellationToken);

        var keys = counts.Select(c => (c.LocalDate, c.BranchId))
            .Concat(AllRows(money).Select(m => (LocalDate: m.Date, m.BranchId)))
            .Distinct()
            .ToList();
        var names = await _clinic.GetBranchNamesAsync(keys.Select(k => k.BranchId).Distinct().ToList(), cancellationToken);

        var rows = keys.Select(k =>
            {
                var row = DailyRow(AppointmentStats.Summarize(counts.Where(c => c.LocalDate == k.LocalDate && c.BranchId == k.BranchId)),
                    Filter(money, r => r.Date == k.LocalDate && r.BranchId == k.BranchId));
                row.Date = Format(k.LocalDate);
                row.BranchId = k.BranchId;
                row.Branch = names.GetValueOrDefault(k.BranchId, "");
                return row;
            })
            .OrderBy(r => r.Date).ThenBy(r => r.Branch)
            .ToList();
        var totals = DailyRow(AppointmentStats.Summarize(counts), money);

        return await OutputAsync("daily-branches", "Report.DailyBranches", model, rows, totals,
        [
            new("Report.Col.Date", r => r.Date),
            new("Report.Col.Branch", r => r.Branch),
            new("Report.Col.Appointments", r => r.Appointments, ReportValueKind.Integer),
            new("Report.Col.Completed", r => r.Completed, ReportValueKind.Integer),
            new("Report.Col.NoShows", r => r.NoShows, ReportValueKind.Integer),
            new("Report.Col.Cancelled", r => r.Cancelled, ReportValueKind.Integer),
            new("Report.Col.NoShowRate", r => r.NoShowRate, ReportValueKind.Percent),
            new("Report.Col.Revenue", r => r.Revenue, ReportValueKind.Money),
            new("Report.Col.Collections", r => r.Collections, ReportValueKind.Money),
        ], cancellationToken);
    }

    /// <summary>One row per local day of the period (days without money included): billed vs collected, payments by method.</summary>
    public async Task<ReportOutput> Revenue(ReportModel model, CancellationToken cancellationToken)
    {
        var money = await _billing.GetMoneyFlowsByLocalDayAsync(model.DateFrom, model.DateTo, BranchesInScope(model), cancellationToken);

        var rows = new List<RevenueReportRowDto>();
        for (var day = model.DateFrom; day <= model.DateTo; day = day.AddDays(1))
        {
            var current = day;
            var row = RevenueRow(Filter(money, r => r.Date == current));
            row.Date = Format(day);
            rows.Add(row);
        }

        return await OutputAsync("revenue", "Report.Revenue", model, rows, RevenueRow(money),
        [
            new("Report.Col.Date", r => r.Date),
            new("Report.Col.Invoiced", r => r.Invoiced, ReportValueKind.Money),
            new("Report.Col.Credited", r => r.Credited, ReportValueKind.Money),
            new("Report.Col.Revenue", r => r.Revenue, ReportValueKind.Money),
            new("Report.Col.Cash", r => r.Cash, ReportValueKind.Money),
            new("Report.Col.Card", r => r.Card, ReportValueKind.Money),
            new("Report.Col.Wallet", r => r.Wallet, ReportValueKind.Money),
            new("Report.Col.Online", r => r.Online, ReportValueKind.Money),
            new("Report.Col.Refunded", r => r.Refunded, ReportValueKind.Money),
            new("Report.Col.Collections", r => r.Collections, ReportValueKind.Money),
            new("Report.Col.Difference", r => r.Difference, ReportValueKind.Money),
        ], cancellationToken);
    }

    /// <summary>One row per doctor: appointments, no-show rate and utilization (booked minutes / scheduled minutes minus time off).</summary>
    public async Task<ReportOutput> Doctors(ReportModel model, CancellationToken cancellationToken)
    {
        var branches = BranchesInScope(model);
        var counts = await _scheduling.CountAppointmentsAsync(model.DateFrom, model.DateTo, branches, model.DoctorId, cancellationToken);
        var capacity = await _clinic.GetDoctorCapacityAsync(model.DateFrom, model.DateTo, branches, model.DoctorId, cancellationToken);

        var doctorIds = counts.Select(c => c.DoctorId).Concat(capacity.Select(c => c.DoctorId)).Distinct().ToList();
        var names = await _clinic.GetDoctorNamesAsync(doctorIds, cancellationToken);

        var rows = doctorIds.Select(id =>
            {
                var row = DoctorRow(AppointmentStats.Summarize(counts.Where(c => c.DoctorId == id)), capacity.Where(c => c.DoctorId == id).Sum(c => c.Minutes));
                row.DoctorId = id;
                row.Doctor = names.GetValueOrDefault(id, "");
                return row;
            })
            .OrderByDescending(r => r.Appointments).ThenBy(r => r.Doctor)
            .ToList();
        var totals = DoctorRow(AppointmentStats.Summarize(counts), capacity.Sum(c => c.Minutes));

        return await OutputAsync("doctors", "Report.Doctors", model, rows, totals,
        [
            new("Report.Col.Doctor", r => r.Doctor),
            new("Report.Col.Appointments", r => r.Appointments, ReportValueKind.Integer),
            new("Report.Col.Completed", r => r.Completed, ReportValueKind.Integer),
            new("Report.Col.NoShows", r => r.NoShows, ReportValueKind.Integer),
            new("Report.Col.Cancelled", r => r.Cancelled, ReportValueKind.Integer),
            new("Report.Col.NoShowRate", r => r.NoShowRate, ReportValueKind.Percent),
            new("Report.Col.AvailableMinutes", r => r.AvailableMinutes, ReportValueKind.Integer),
            new("Report.Col.BookedMinutes", r => r.BookedMinutes, ReportValueKind.Integer),
            new("Report.Col.Utilization", r => r.Utilization, ReportValueKind.Percent),
        ], cancellationToken);
    }

    private static DailyBranchReportRowDto DailyRow(AppointmentStats.Summary s, MoneyFlows money) => new()
    {
        Appointments = s.Booked,
        Completed = s.Completed,
        NoShows = s.NoShows,
        Cancelled = s.Cancelled,
        NoShowRate = s.NoShowRate,
        Revenue = Sum(money.Invoiced) - Sum(money.Credited),
        Collections = Sum(money.Payments) - Sum(money.Refunds),
    };

    private static RevenueReportRowDto RevenueRow(MoneyFlows money)
    {
        decimal ByMethod(PaymentMethodEnum method) => money.Payments.Where(p => p.Method == method).Sum(p => p.Amount);
        var revenue = Sum(money.Invoiced) - Sum(money.Credited);
        var collections = Sum(money.Payments) - Sum(money.Refunds);
        return new RevenueReportRowDto
        {
            Invoiced = Sum(money.Invoiced),
            Credited = Sum(money.Credited),
            Revenue = revenue,
            Cash = ByMethod(PaymentMethodEnum.Cash),
            Card = ByMethod(PaymentMethodEnum.Card),
            Wallet = ByMethod(PaymentMethodEnum.Wallet),
            Online = ByMethod(PaymentMethodEnum.Online),
            Refunded = Sum(money.Refunds),
            Collections = collections,
            Difference = revenue - collections,
        };
    }

    private static DoctorReportRowDto DoctorRow(AppointmentStats.Summary s, int availableMinutes) => new()
    {
        Appointments = s.Booked,
        Completed = s.Completed,
        NoShows = s.NoShows,
        Cancelled = s.Cancelled,
        NoShowRate = s.NoShowRate,
        AvailableMinutes = availableMinutes,
        BookedMinutes = s.BookedMinutes,
        Utilization = availableMinutes == 0 ? 0 : Math.Round(s.BookedMinutes * 100m / availableMinutes, 1),
    };

    private async Task<ReportOutput> OutputAsync<TRow>(string key, string titleKey, ReportModel model, List<TRow> rows, TRow totals,
        IReadOnlyList<ReportColumn<TRow>> columns, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var title = _localizer[titleKey].Value;
        if (model.Format == ReportFormatEnum.Json)
        {
            var dto = new ReportDto<TRow> { Title = title, DateFrom = Format(model.DateFrom), DateTo = Format(model.DateTo), GeneratedAt = now, Rows = rows, Totals = totals };
            return new ReportOutput { Json = new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: dto) };
        }

        var clinic = (await _tenancy.GetTenantAsync(_tenant.TenantId ?? throw new InvalidOperationException("No tenant."), cancellationToken))?.Name ?? "";
        var totalsRow = columns.Select(c => c.Kind == ReportValueKind.Text ? null : c.Value(totals)).ToArray();
        var document = new ReportDocument
        {
            Clinic = clinic,
            Title = title,
            Period = string.Format(_localizer["Report.Period"].Value, Format(model.DateFrom), Format(model.DateTo)),
            Generated = string.Format(_localizer["Report.Generated"].Value, now.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)),
            TotalLabel = _localizer["Report.Total"].Value,
            RightToLeft = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar",
            Columns = columns.Select(c => (_localizer[c.HeaderKey].Value, c.Kind)).ToList(),
            Rows = rows.Select(r => columns.Select(c => c.Value(r)).ToArray()).ToList(),
            Totals = totalsRow,
        };

        var pdf = model.Format == ReportFormatEnum.Pdf;
        var content = pdf ? PdfReportWriter.Write(document) : ExcelReportWriter.Write(document);
        var fileName = $"{key}-{model.DateFrom:yyyyMMdd}-{model.DateTo:yyyyMMdd}.{(pdf ? "pdf" : "xlsx")}";

        // Exports leave the system: who exported what and for which period (security-design.md).
        await _audit.LogAsync("Reports.Exported", detail: $"{key} {model.Format} {Format(model.DateFrom)}..{Format(model.DateTo)}",
            cancellationToken: CancellationToken.None);
        return new ReportOutput { Content = content, ContentType = pdf ? PdfType : XlsxType, FileName = fileName };
    }

    /// <summary>Null = every branch (caller sees all); otherwise the requested branches limited to the caller's scope.</summary>
    private IReadOnlyCollection<long>? BranchesInScope(ReportModel model)
    {
        var requested = model.BranchIds is { Count: > 0 } ? model.BranchIds.Distinct().ToList() : null;
        if (_access.HasAllBranches)
            return requested;

        var scope = _access.BranchIds.ToList();
        return requested is null ? scope : requested.Where(scope.Contains).ToList();
    }

    private static IEnumerable<MoneyRow> AllRows(MoneyFlows money) => money.Invoiced.Concat(money.Credited).Concat(money.Payments).Concat(money.Refunds);

    private static MoneyFlows Filter(MoneyFlows money, Func<MoneyRow, bool> keep) =>
        new(money.Invoiced.Where(keep).ToList(), money.Credited.Where(keep).ToList(), money.Payments.Where(keep).ToList(), money.Refunds.Where(keep).ToList());

    private static decimal Sum(IEnumerable<MoneyRow> rows) => rows.Sum(r => r.Amount);

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
