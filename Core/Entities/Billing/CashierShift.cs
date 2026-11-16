using Core.Entities.Common;
using Core.Enums.Billing;

namespace Core.Entities.Billing;

/// <summary>
/// A cashier's drawer session at one branch (ADR-011). Payments and refunds belong to an open shift; every one of them
/// also updates this row (rowversion), so closing can't race a payment. A closed shift never changes.
/// </summary>
public class CashierShift : TenantOwnedEntity
{
    public long BranchId { get; set; }
    public long CashierUserId { get; set; }
    public ShiftStatusEnum Status { get; private set; } = ShiftStatusEnum.Open;
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public decimal OpeningCash { get; set; }

    /// <summary>OpeningCash + cash payments − cash refunds, computed when closing.</summary>
    public decimal? ExpectedCash { get; private set; }
    public decimal? CountedCash { get; private set; }

    /// <summary>Counted − Expected (negative = cash missing).</summary>
    public decimal? Variance { get; private set; }
    public long? ClosedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    public bool IsOpen => Status == ShiftStatusEnum.Open;

    public bool TryClose(decimal expectedCash, decimal countedCash, long closedByUserId, string? notes, DateTimeOffset now)
    {
        if (!IsOpen)
            return false;

        Status = ShiftStatusEnum.Closed;
        ClosedAt = now;
        ExpectedCash = expectedCash;
        CountedCash = countedCash;
        Variance = countedCash - expectedCash;
        ClosedByUserId = closedByUserId;
        Notes = notes;
        return true;
    }
}
