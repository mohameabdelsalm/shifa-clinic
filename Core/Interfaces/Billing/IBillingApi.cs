using Core.DTO.Billing;

namespace Core.Interfaces.Billing;

/// <summary>What other modules may ask Billing (ADR-011).</summary>
public interface IBillingApi
{
    /// <summary>
    /// Creates the visit invoice of a checked-in appointment with the current price, or returns the existing one
    /// (idempotent: safe to call again after a crash or a repeated check-in). Returns the invoice id.
    /// </summary>
    /// <remarks>Returns null (and creates nothing) when the Billing feature is off for the tenant.</remarks>
    Task<long?> EnsureVisitInvoiceAsync(VisitInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Money of [fromUtc, toUtc) for statistics: invoiced (non-void), credited, paid and refunded, per UTC day and branch.
    /// <paramref name="branchIds"/> null = all branches. <paramref name="cashierUserId"/> limits payments/refunds to one collector.
    /// </summary>
    Task<MoneyFlows> GetMoneyFlowsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, IReadOnlyCollection<long>? branchIds,
        long? cashierUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Like <see cref="GetMoneyFlowsAsync"/>, but each amount is dated by the local day of its branch's time zone, for the
    /// local days [from, to] (reports: a payment at 01:00 Cairo time belongs to that Cairo day, DST included).
    /// </summary>
    Task<MoneyFlows> GetMoneyFlowsByLocalDayAsync(DateOnly fromLocalDate, DateOnly toLocalDate, IReadOnlyCollection<long>? branchIds,
        CancellationToken cancellationToken = default);
}
