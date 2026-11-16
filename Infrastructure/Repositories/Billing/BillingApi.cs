using System.Net;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.DTO.Tenancy;
using Core.Enums.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Interfaces.Tenancy;
using Infrastructure.Configurations.Billing;
using Infrastructure.Data;
using Infrastructure.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Billing;

public sealed class BillingApi : IBillingApi
{
    private readonly BillingDbContext _context;
    private readonly InvoiceIssuer _issuer;
    private readonly IClinicApi _clinic;
    private readonly ITenantContext _tenant;
    private readonly ITenancyApi _tenancy;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public BillingApi(BillingDbContext context, InvoiceIssuer issuer, IClinicApi clinic, ITenantContext tenant, ITenancyApi tenancy,
        IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _issuer = issuer;
        _clinic = clinic;
        _tenant = tenant;
        _tenancy = tenancy;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<long?> EnsureVisitInvoiceAsync(VisitInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        // A clinic without the Billing feature checks patients in without invoices.
        if (_tenant.TenantId is not long tenantId || !await _tenancy.IsFeatureEnabledAsync(tenantId, FeatureCodes.Billing, cancellationToken))
            return null;

        var existing = await ExistingVisitInvoiceAsync(request.AppointmentId, cancellationToken);
        if (existing is not null)
            return existing.Value;

        // Rule 8: the price at check-in time.
        var price = await _clinic.GetCurrentPriceAsync(request.ServiceId, request.BranchId, cancellationToken)
            ?? throw new BusinessException(_localizer["ServiceHasNoPrice"].Value, HttpStatusCode.BadRequest);

        try
        {
            var invoice = await _issuer.IssueAsync(InvoiceKindEnum.Visit, request.PatientId, request.BranchId, request.AppointmentId,
                [(price, 1)], cancellationToken);
            await _audit.LogAsync("Billing.InvoiceIssued", "Invoice", invoice.Id, detail: invoice.Number, cancellationToken: CancellationToken.None);
            return invoice.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains(BillingIndexes.VisitInvoice, StringComparison.Ordinal) == true)
        {
            // A concurrent check-in (or the repair endpoint) created it first.
            _context.ChangeTracker.Clear();
            return (await ExistingVisitInvoiceAsync(request.AppointmentId, cancellationToken))!.Value;
        }
    }

    public async Task<MoneyFlows> GetMoneyFlowsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, IReadOnlyCollection<long>? branchIds,
        long? cashierUserId, CancellationToken cancellationToken = default)
    {
        var ids = branchIds?.ToList();
        var invoices = _context.Invoices.AsNoTracking().Where(i => ids == null || ids.Contains(i.BranchId));

        var invoiced = await invoices
            .Where(i => i.IssuedAt >= fromUtc && i.IssuedAt < toUtc && i.Status != InvoiceStatusEnum.Void)
            .Select(i => new { At = i.IssuedAt, i.BranchId, Amount = i.Total })
            .ToListAsync(cancellationToken);
        var credited = await (from c in _context.CreditNotes.AsNoTracking()
                              join i in invoices on c.InvoiceId equals i.Id
                              where c.CreationDate >= fromUtc && c.CreationDate < toUtc
                              select new { At = c.CreationDate, i.BranchId, c.Amount }).ToListAsync(cancellationToken);
        var payments = await (from p in _context.Payments.AsNoTracking()
                              join i in invoices on p.InvoiceId equals i.Id
                              where p.PaidAt >= fromUtc && p.PaidAt < toUtc && (cashierUserId == null || p.ReceivedByUserId == cashierUserId)
                              select new { At = p.PaidAt, i.BranchId, p.Method, p.Amount }).ToListAsync(cancellationToken);
        var refunds = await (from r in _context.Refunds.AsNoTracking()
                             join p in _context.Payments.AsNoTracking() on r.PaymentId equals p.Id
                             join i in invoices on p.InvoiceId equals i.Id
                             where r.CreationDate >= fromUtc && r.CreationDate < toUtc && (cashierUserId == null || r.RefundedByUserId == cashierUserId)
                             select new { At = r.CreationDate, i.BranchId, r.Method, r.Amount }).ToListAsync(cancellationToken);

        static DateOnly Day(DateTimeOffset at) => DateOnly.FromDateTime(at.UtcDateTime);
        return new MoneyFlows(
            invoiced.Select(x => new MoneyRow(Day(x.At), x.BranchId, null, x.Amount)).ToList(),
            credited.Select(x => new MoneyRow(Day(x.At), x.BranchId, null, x.Amount)).ToList(),
            payments.Select(x => new MoneyRow(Day(x.At), x.BranchId, x.Method, x.Amount)).ToList(),
            refunds.Select(x => new MoneyRow(Day(x.At), x.BranchId, x.Method, x.Amount)).ToList());
    }

    private Task<long?> ExistingVisitInvoiceAsync(long appointmentId, CancellationToken cancellationToken) =>
        _context.Invoices.Where(i => i.AppointmentId == appointmentId && i.Kind == InvoiceKindEnum.Visit)
            .Select(i => (long?)i.Id).FirstOrDefaultAsync(cancellationToken);
}
