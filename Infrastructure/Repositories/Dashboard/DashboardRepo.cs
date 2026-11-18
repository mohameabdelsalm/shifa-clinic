using System.Globalization;
using System.Net;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.DTO.Dashboard;
using Core.DTO.Scheduling;
using Core.Enums.Billing;
using Core.Enums.Scheduling;
using Core.Interfaces.Billing;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Interfaces.Dashboard;
using Core.Interfaces.Patients;
using Core.Interfaces.Scheduling;
using Core.Models.Dashboard;
using Infrastructure.Services.Reporting;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Dashboard;

/// <summary>
/// Dashboard statistics (Trio HRM pattern: statistics, timeline, breakdowns). It reads no database itself: it asks the
/// modules for compact data (<see cref="ISchedulingApi.CountAppointmentsAsync"/>, patient registration times, names)
/// and computes the numbers. Branch scope applies: a branch-limited member only sees their branches.
/// </summary>
public class DashboardRepo : IDashboardRepo
{
    private const int DefaultDays = 30;
    private const int DailyTimelineMaxDays = 31;

    private readonly ISchedulingApi _scheduling;
    private readonly IPatientsApi _patients;
    private readonly IClinicApi _clinic;
    private readonly IBillingApi _billing;
    private readonly IAccessContext _access;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public DashboardRepo(ISchedulingApi scheduling, IPatientsApi patients, IClinicApi clinic, IBillingApi billing, IAccessContext access,
        TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _scheduling = scheduling;
        _patients = patients;
        _clinic = clinic;
        _billing = billing;
        _access = access;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Statistics(DashboardModel model, CancellationToken cancellationToken)
    {
        var (from, to) = Period(model);
        var (prevFrom, prevTo) = (from.AddYears(-1), to.AddYears(-1));
        var branches = BranchesInScope(model);

        var current = Summarize(await _scheduling.CountAppointmentsAsync(from, to, branches, model.DoctorId, cancellationToken));
        var previous = Summarize(await _scheduling.CountAppointmentsAsync(prevFrom, prevTo, branches, model.DoctorId, cancellationToken));
        var newPatients = (await _patients.GetRegistrationTimesAsync(StartUtc(from), StartUtc(to.AddDays(1)), cancellationToken)).Count;
        var previousNewPatients = (await _patients.GetRegistrationTimesAsync(StartUtc(prevFrom), StartUtc(prevTo.AddDays(1)), cancellationToken)).Count;
        var money = await _billing.GetMoneyFlowsAsync(StartUtc(from), StartUtc(to.AddDays(1)), branches, null, cancellationToken);
        var previousMoney = await _billing.GetMoneyFlowsAsync(StartUtc(prevFrom), StartUtc(prevTo.AddDays(1)), branches, null, cancellationToken);

        var vsPeriod = PeriodLabel(prevFrom, prevTo);
        var statistics = new DashboardStatisticsDto
        {
            DateFrom = Format(from),
            DateTo = Format(to),
            Appointments = Kpi(current.Booked, previous.Booked, vsPeriod),
            CompletedVisits = Kpi(current.Completed, previous.Completed, vsPeriod),
            Cancellations = Kpi(current.Cancelled, previous.Cancelled, vsPeriod, lowerIsBetter: true),
            NoShowRate = Kpi(current.NoShowRate, previous.NoShowRate, vsPeriod, lowerIsBetter: true),
            NewPatients = Kpi(newPatients, previousNewPatients, vsPeriod),
            Revenue = Kpi(Revenue(money), Revenue(previousMoney), vsPeriod),
            Collections = Kpi(Collections(money), Collections(previousMoney), vsPeriod),
        };
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: statistics);
    }

    /// <summary>Collections of the period per payment method, with a daily (≤ 31 days) or monthly timeline.</summary>
    public async Task<Response> Payments(PaymentsDashboardModel model, CancellationToken cancellationToken)
    {
        var (from, to) = Period(model);
        var money = await _billing.GetMoneyFlowsAsync(StartUtc(from), StartUtc(to.AddDays(1)), BranchesInScope(model), model.CashierUserId,
            cancellationToken);

        var daily = to.DayNumber - from.DayNumber + 1 <= DailyTimelineMaxDays;
        var timeline = new List<MoneyPointDto>();
        for (var bucketStart = daily ? from : new DateOnly(from.Year, from.Month, 1); bucketStart <= to;
             bucketStart = daily ? bucketStart.AddDays(1) : bucketStart.AddMonths(1))
        {
            var bucketFrom = bucketStart < from ? from : bucketStart;
            var bucketEnd = daily ? bucketStart : bucketStart.AddMonths(1).AddDays(-1);
            var bucketTo = bucketEnd > to ? to : bucketEnd;
            var point = MoneyPoint(money, bucketFrom, bucketTo);
            point.Period = daily ? Format(bucketStart) : bucketStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            point.Label = bucketStart.ToString(daily ? "dd MMM" : "MMM yyyy", CultureInfo.InvariantCulture);
            timeline.Add(point);
        }

        var totals = MoneyPoint(money, from, to);
        totals.Period = $"{Format(from)}..{Format(to)}";
        totals.Label = PeriodLabel(from, to);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: new PaymentsDashboardDto { Totals = totals, Timeline = timeline });
    }

    private static MoneyPointDto MoneyPoint(MoneyFlows money, DateOnly from, DateOnly to)
    {
        var payments = money.Payments.Where(r => r.Date >= from && r.Date <= to).ToList();
        decimal ByMethod(PaymentMethodEnum method) => payments.Where(p => p.Method == method).Sum(p => p.Amount);

        var collected = payments.Sum(p => p.Amount);
        var refunded = money.Refunds.Where(r => r.Date >= from && r.Date <= to).Sum(r => r.Amount);
        return new MoneyPointDto
        {
            Cash = ByMethod(PaymentMethodEnum.Cash),
            Card = ByMethod(PaymentMethodEnum.Card),
            Wallet = ByMethod(PaymentMethodEnum.Wallet),
            Online = ByMethod(PaymentMethodEnum.Online),
            Collected = collected,
            Refunded = refunded,
            Net = collected - refunded,
            PaymentsCount = payments.Count,
        };
    }

    private static decimal Revenue(MoneyFlows money) => money.Invoiced.Sum(r => r.Amount) - money.Credited.Sum(r => r.Amount);

    private static decimal Collections(MoneyFlows money) => money.Payments.Sum(r => r.Amount) - money.Refunds.Sum(r => r.Amount);

    public async Task<Response> Timeline(DashboardModel model, CancellationToken cancellationToken)
    {
        var (from, to) = Period(model);
        var rows = await _scheduling.CountAppointmentsAsync(from, to, BranchesInScope(model), model.DoctorId, cancellationToken);
        var registrations = (await _patients.GetRegistrationTimesAsync(StartUtc(from), StartUtc(to.AddDays(1)), cancellationToken))
            .Select(t => DateOnly.FromDateTime(t.UtcDateTime)).ToList();

        // Daily points for short periods, monthly otherwise (HRM uses months).
        var daily = to.DayNumber - from.DayNumber + 1 <= DailyTimelineMaxDays;
        var points = new List<TimelinePointDto>();
        for (var bucketStart = daily ? from : new DateOnly(from.Year, from.Month, 1); bucketStart <= to;
             bucketStart = daily ? bucketStart.AddDays(1) : bucketStart.AddMonths(1))
        {
            var bucketFrom = bucketStart < from ? from : bucketStart;
            var bucketEnd = daily ? bucketStart : bucketStart.AddMonths(1).AddDays(-1);
            var bucketTo = bucketEnd > to ? to : bucketEnd;

            var summary = Summarize(rows.Where(r => r.LocalDate >= bucketFrom && r.LocalDate <= bucketTo));
            points.Add(new TimelinePointDto
            {
                Period = daily ? Format(bucketStart) : bucketStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Label = bucketStart.ToString(daily ? "dd MMM" : "MMM yyyy", CultureInfo.InvariantCulture),
                DateFrom = Format(bucketFrom),
                DateTo = Format(bucketTo),
                Appointments = summary.Booked,
                Completed = summary.Completed,
                NoShows = summary.NoShows,
                Cancelled = summary.Cancelled,
                NewPatients = registrations.Count(d => d >= bucketFrom && d <= bucketTo),
            });
        }

        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: points);
    }

    public async Task<Response> Branches(DashboardModel model, CancellationToken cancellationToken)
    {
        var (from, to) = Period(model);
        var rows = await _scheduling.CountAppointmentsAsync(from, to, BranchesInScope(model), model.DoctorId, cancellationToken);
        var groups = rows.GroupBy(r => r.BranchId).ToList();
        var names = await _clinic.GetBranchNamesAsync(groups.Select(g => g.Key).ToList(), cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: Breakdown(groups, names));
    }

    public async Task<Response> Doctors(DashboardModel model, CancellationToken cancellationToken)
    {
        var (from, to) = Period(model);
        var rows = await _scheduling.CountAppointmentsAsync(from, to, BranchesInScope(model), model.DoctorId, cancellationToken);
        var groups = rows.GroupBy(r => r.DoctorId).ToList();
        var names = await _clinic.GetDoctorNamesAsync(groups.Select(g => g.Key).ToList(), cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: Breakdown(groups, names));
    }

    private static List<BreakdownRowDto> Breakdown(IEnumerable<IGrouping<long, AppointmentCountRow>> groups, IReadOnlyDictionary<long, string> names) =>
        groups.Select(g =>
            {
                var summary = Summarize(g);
                return new BreakdownRowDto
                {
                    Id = g.Key,
                    Name = names.TryGetValue(g.Key, out var name) ? name : "",
                    Appointments = summary.Booked,
                    Completed = summary.Completed,
                    NoShows = summary.NoShows,
                    Cancelled = summary.Cancelled,
                    NoShowRate = summary.NoShowRate,
                };
            })
            .OrderByDescending(r => r.Appointments).ThenBy(r => r.Name)
            .ToList();

    /// <summary>Shared with the reports (<see cref="AppointmentStats"/>), so both always agree.</summary>
    private static AppointmentStats.Summary Summarize(IEnumerable<AppointmentCountRow> rows) => AppointmentStats.Summarize(rows);

    private static KpiDto Kpi(decimal current, decimal previous, string vsPeriod, bool lowerIsBetter = false) => new()
    {
        Total = current,
        Previous = previous,
        VsPeriod = vsPeriod,
        ChangePercent = previous == 0 ? (current == 0 ? 0 : 100) : Math.Round((current - previous) * 100 / previous, 1),
        IsPositive = lowerIsBetter ? current <= previous : current >= previous,
    };

    private (DateOnly From, DateOnly To) Period(DashboardModel model)
    {
        var to = model.DateTo ?? DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        var from = model.DateFrom ?? to.AddDays(-DefaultDays);
        return (from, to);
    }

    /// <summary>Null = every branch (caller sees all); otherwise the requested branches limited to the caller's scope.</summary>
    private IReadOnlyCollection<long>? BranchesInScope(DashboardModel model)
    {
        var requested = model.BranchIds is { Count: > 0 } ? model.BranchIds.Distinct().ToList() : null;
        if (_access.HasAllBranches)
            return requested;

        var scope = _access.BranchIds.ToList();
        return requested is null ? scope : requested.Where(scope.Contains).ToList();
    }

    private static DateTimeOffset StartUtc(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string PeriodLabel(DateOnly from, DateOnly to)
    {
        var fromLabel = from.ToString("MMM yyyy", CultureInfo.InvariantCulture);
        var toLabel = to.ToString("MMM yyyy", CultureInfo.InvariantCulture);
        return fromLabel == toLabel ? fromLabel : $"{fromLabel} - {toLabel}";
    }
}
