using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Models.Billing;
using Infrastructure.Configurations.Billing;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Billing;

/// <summary>
/// Cashier shifts (ADR-011): one open shift per cashier and branch; payments and refunds need the caller's open shift.
/// Closing stores expected cash (opening + cash in − cash out) and the variance against the counted cash.
/// Branch scope applies (another branch's shift = 404).
/// </summary>
public class CashierShiftRepo : ICashierShiftRepo
{
    private readonly BillingDbContext _context;
    private readonly IMapper _mapper;
    private readonly IClinicApi _clinic;
    private readonly IAccessContext _access;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public CashierShiftRepo(BillingDbContext context, IMapper mapper, IClinicApi clinic, IAccessContext access, ICurrentUser currentUser,
        IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _clinic = clinic;
        _access = access;
        _currentUser = currentUser;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Open(OpenShiftModel model, CancellationToken cancellationToken)
    {
        if (!_access.CanAccessBranch(model.BranchId) || (await _clinic.GetExistingBranchIdsAsync([model.BranchId], cancellationToken)).Count == 0)
            throw new BusinessException(_localizer["BranchNotFound"].Value, HttpStatusCode.NotFound);

        var shift = new CashierShift
        {
            BranchId = model.BranchId,
            CashierUserId = CurrentUserId(),
            OpenedAt = _time.GetUtcNow(),
            OpeningCash = model.OpeningCash,
        };
        _context.CashierShifts.Add(shift);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains(BillingIndexes.OpenShift, StringComparison.Ordinal) == true)
        {
            throw new BusinessException(_localizer["ShiftAlreadyOpen"].Value, HttpStatusCode.BadRequest);
        }

        await _audit.LogAsync("Billing.ShiftOpened", "CashierShift", shift.Id, detail: $"opening {shift.OpeningCash}", cancellationToken: CancellationToken.None);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: shift.Id);
    }

    /// <summary>The caller's open shift at the branch, with live totals (data is null when there is none).</summary>
    public async Task<Response> Current(long branchId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var shift = await InScope(_context.CashierShifts.AsNoTracking())
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.CashierUserId == userId && s.Status == ShiftStatusEnum.Open, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: shift is null ? null : await ToDtoAsync(shift, cancellationToken));
    }

    public async Task<Response> Close(CloseShiftModel model, CancellationToken cancellationToken)
    {
        var shift = await InScope(_context.CashierShifts).FirstOrDefaultAsync(s => s.Id == model.ShiftId, cancellationToken) ?? throw NotFound();
        var userId = CurrentUserId();

        // A cashier closes their own shift; a Billing.Refund holder (manager) can close any shift in their branches.
        if (shift.CashierUserId != userId && !_access.HasPermission(Permissions.BillingRefund))
            throw new BusinessException(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);
        if (!shift.IsOpen)
            throw new BusinessException(_localizer["ShiftClosed"].Value, HttpStatusCode.BadRequest);

        var totals = await TotalsAsync([shift.Id], cancellationToken);
        var t = totals.GetValueOrDefault(shift.Id) ?? ShiftTotals.Empty;
        shift.TryClose(shift.OpeningCash + t.CashIn - t.CashOut, model.CountedCash, userId, model.Notes?.Trim(), _time.GetUtcNow());

        // The rowversion makes this fail (409) if a payment or refund was recorded in the shift meanwhile.
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Billing.ShiftClosed", "CashierShift", shift.Id,
            detail: $"expected {shift.ExpectedCash} counted {shift.CountedCash} variance {shift.Variance}", cancellationToken: CancellationToken.None);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK, data: await ToDtoAsync(shift, cancellationToken));
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var shift = await InScope(_context.CashierShifts.AsNoTracking()).FirstOrDefaultAsync(s => s.Id == id, cancellationToken) ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: await ToDtoAsync(shift, cancellationToken));
    }

    public async Task<Response> Search(SearchShiftsModel model, CancellationToken cancellationToken)
    {
        var query = SortShifts(InScope(_context.CashierShifts.AsNoTracking()).Where(FilterShifts(model)), model);
        var page = await query.ProjectTo<CashierShiftDto>(_mapper.ConfigurationProvider).ToPagedListAsync(model, cancellationToken);

        var totals = await TotalsAsync(page.Items.Select(s => s.Id).ToList(), cancellationToken);
        foreach (var dto in page.Items)
            Fill(dto, totals.GetValueOrDefault(dto.Id) ?? ShiftTotals.Empty);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: page);
    }

    private static Expression<Func<CashierShift, bool>> FilterShifts(SearchShiftsModel model)
    {
        DateTimeOffset? fromUtc = model.FromDate is DateOnly from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        DateTimeOffset? toUtc = model.ToDate is DateOnly to ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        return s => (model.BranchId == null || s.BranchId == model.BranchId)
                    && (model.CashierUserId == null || s.CashierUserId == model.CashierUserId)
                    && (model.Status == null || s.Status == model.Status)
                    && (fromUtc == null || s.OpenedAt >= fromUtc)
                    && (toUtc == null || s.OpenedAt < toUtc);
    }

    private static IQueryable<CashierShift> SortShifts(IQueryable<CashierShift> query, SearchShiftsModel model) => model.SortBy switch
    {
        "closedAt" => query.OrderBy(model.SortType, s => s.ClosedAt).ThenBy(s => s.Id),
        "variance" => query.OrderBy(model.SortType, s => s.Variance).ThenBy(s => s.Id),
        "status" => query.OrderBy(model.SortType, s => s.Status).ThenBy(s => s.Id),
        _ => query.OrderBy(model.SortType, s => s.OpenedAt).ThenBy(s => s.Id),
    };

    private sealed record ShiftTotals(decimal CashIn, decimal CardIn, decimal WalletIn, decimal CashOut, decimal NonCashOut, int PaymentsCount)
    {
        public static readonly ShiftTotals Empty = new(0, 0, 0, 0, 0, 0);
    }

    /// <summary>Money in and out per shift, grouped in SQL.</summary>
    private async Task<Dictionary<long, ShiftTotals>> TotalsAsync(IReadOnlyCollection<long> shiftIds, CancellationToken cancellationToken)
    {
        if (shiftIds.Count == 0)
            return [];
        var ids = shiftIds.ToList();

        var payments = await _context.Payments.AsNoTracking().Where(p => p.CashierShiftId != null && ids.Contains(p.CashierShiftId.Value))
            .GroupBy(p => new { ShiftId = p.CashierShiftId!.Value, p.Method })
            .Select(g => new { g.Key.ShiftId, g.Key.Method, Amount = g.Sum(p => p.Amount), Count = g.Count() })
            .ToListAsync(cancellationToken);
        var refunds = await _context.Refunds.AsNoTracking().Where(r => r.CashierShiftId != null && ids.Contains(r.CashierShiftId.Value))
            .GroupBy(r => new { ShiftId = r.CashierShiftId!.Value, r.Method })
            .Select(g => new { g.Key.ShiftId, g.Key.Method, Amount = g.Sum(r => r.Amount) })
            .ToListAsync(cancellationToken);

        return ids.ToDictionary(id => id, id => new ShiftTotals(
            payments.Where(p => p.ShiftId == id && p.Method == PaymentMethodEnum.Cash).Sum(p => p.Amount),
            payments.Where(p => p.ShiftId == id && p.Method == PaymentMethodEnum.Card).Sum(p => p.Amount),
            payments.Where(p => p.ShiftId == id && p.Method == PaymentMethodEnum.Wallet).Sum(p => p.Amount),
            refunds.Where(r => r.ShiftId == id && r.Method == PaymentMethodEnum.Cash).Sum(r => r.Amount),
            refunds.Where(r => r.ShiftId == id && r.Method != PaymentMethodEnum.Cash).Sum(r => r.Amount),
            payments.Where(p => p.ShiftId == id).Sum(p => p.Count)));
    }

    private async Task<CashierShiftDto> ToDtoAsync(CashierShift shift, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<CashierShiftDto>(shift);
        Fill(dto, (await TotalsAsync([shift.Id], cancellationToken)).GetValueOrDefault(shift.Id) ?? ShiftTotals.Empty);
        return dto;
    }

    private static void Fill(CashierShiftDto dto, ShiftTotals totals)
    {
        dto.CashIn = totals.CashIn;
        dto.CardIn = totals.CardIn;
        dto.WalletIn = totals.WalletIn;
        dto.CashOut = totals.CashOut;
        dto.NonCashOut = totals.NonCashOut;
        dto.PaymentsCount = totals.PaymentsCount;
        dto.ExpectedCash ??= dto.OpeningCash + totals.CashIn - totals.CashOut;   // live value while open
    }

    private IQueryable<CashierShift> InScope(IQueryable<CashierShift> shifts)
    {
        if (_access.HasAllBranches)
            return shifts;
        var scope = _access.BranchIds.ToList();
        return shifts.Where(s => scope.Contains(s.BranchId));
    }

    private long CurrentUserId() => _currentUser.GetUserId() ?? throw new InvalidOperationException("No signed-in user.");

    private BusinessException NotFound() => new(_localizer["ShiftNotFound"].Value, HttpStatusCode.NotFound);
}
