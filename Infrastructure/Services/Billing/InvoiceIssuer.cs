using Core.DTO.Clinic;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Interfaces.Common;
using Core.Interfaces.Tenancy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Billing;

/// <summary>
/// Creates an invoice with its items from price-list snapshots, in one transaction with its number (ADR-011).
/// Shared by the visit invoice (check-in) and extra invoices. The caller handles unique violations.
/// </summary>
public sealed class InvoiceIssuer
{
    private readonly BillingDbContext _context;
    private readonly BillingNumbers _numbers;
    private readonly ITenantContext _tenant;
    private readonly ITenancyApi _tenancy;
    private readonly TimeProvider _time;

    public InvoiceIssuer(BillingDbContext context, BillingNumbers numbers, ITenantContext tenant, ITenancyApi tenancy, TimeProvider time)
    {
        _context = context;
        _numbers = numbers;
        _tenant = tenant;
        _tenancy = tenancy;
        _time = time;
    }

    public async Task<Invoice> IssueAsync(InvoiceKindEnum kind, long patientId, long branchId, long? appointmentId,
        IReadOnlyList<(ServicePriceSnapshot Price, int Quantity)> lines, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("No tenant context.");
        var currency = (await _tenancy.GetTenantAsync(tenantId, cancellationToken))?.CurrencyCode ?? "EGP";

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var (year, sequence, number) = await _numbers.NextAsync("INV",
            y => _context.Invoices.Where(i => i.Year == y).MaxAsync(i => (int?)i.Sequence, cancellationToken), cancellationToken);

        var invoice = new Invoice
        {
            BranchId = branchId,
            PatientId = patientId,
            AppointmentId = appointmentId,
            Kind = kind,
            Number = number,
            Year = year,
            Sequence = sequence,
            CurrencyCode = currency,
            IssuedAt = _time.GetUtcNow(),
        };
        _context.Invoices.Add(invoice);

        decimal subtotal = 0;
        foreach (var (price, quantity) in lines)
        {
            var lineTotal = Math.Round(price.Price * quantity, 2, MidpointRounding.AwayFromZero);
            subtotal += lineTotal;
            _context.InvoiceItems.Add(new InvoiceItem
            {
                InvoiceId = invoice.Id,
                ServiceId = price.ServiceId,
                Description = price.NameEn,
                Quantity = quantity,
                UnitPrice = price.Price,
                LineTotal = lineTotal,
            });
        }
        invoice.SetTotals(subtotal);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return invoice;
    }
}
