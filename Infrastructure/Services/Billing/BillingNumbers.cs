using Core.Interfaces.Common;
using Infrastructure.Data;

namespace Infrastructure.Services.Billing;

/// <summary>
/// Document numbers per tenant and year: INV-2026-000001, RCT-…, RFD-…, CN-… (ADR-011). "max + 1" under an app lock
/// (<see cref="DatabaseLocks"/>), so concurrent requests take numbers one at a time; the unique index is the final guard.
/// Must be called inside a transaction, and the row saved before that transaction commits.
/// </summary>
public sealed class BillingNumbers
{
    private readonly BillingDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly TimeProvider _time;

    public BillingNumbers(BillingDbContext context, ITenantContext tenant, TimeProvider time)
    {
        _context = context;
        _tenant = tenant;
        _time = time;
    }

    /// <param name="prefix">INV, RCT, RFD or CN.</param>
    /// <param name="maxSequenceForYear">Returns the highest sequence already used in that year (null when none).</param>
    public async Task<(int Year, int Sequence, string Number)> NextAsync(string prefix, Func<int, Task<int?>> maxSequenceForYear,
        CancellationToken cancellationToken)
    {
        var year = _time.GetUtcNow().Year;
        await _context.Database.AcquireAsync($"billing:number:{prefix}:{_tenant.TenantId}:{year}", cancellationToken);
        var sequence = (await maxSequenceForYear(year) ?? 0) + 1;
        return (year, sequence, $"{prefix}-{year}-{sequence:D6}");
    }
}
