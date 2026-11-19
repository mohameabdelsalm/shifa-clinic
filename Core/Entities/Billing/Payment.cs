using Core.Entities.Common;
using Core.Enums.Billing;

namespace Core.Entities.Billing;

/// <summary>Money received for an invoice. Never edited; refunds are separate rows.</summary>
public class Payment : TenantOwnedEntity
{
    public long InvoiceId { get; set; }

    /// <summary>The cashier shift that received it; null for online payments.</summary>
    public long? CashierShiftId { get; set; }

    /// <summary>RCT-yyyy-000001 (receipt number).</summary>
    public string Number { get; set; } = "";
    public int Year { get; set; }
    public int Sequence { get; set; }

    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; private set; }
    public PaymentMethodEnum Method { get; set; }

    /// <summary>POS / wallet reference, or the gateway transaction id for online payments.</summary>
    public string? Reference { get; set; }
    public DateTimeOffset PaidAt { get; set; }

    /// <summary>Staff member who collected it; null for online payments.</summary>
    public long? ReceivedByUserId { get; set; }

    public decimal Refundable => Amount - RefundedAmount;

    public bool TryRefund(decimal amount)
    {
        if (amount <= 0 || amount > Refundable)
            return false;
        RefundedAmount += amount;
        return true;
    }

    /// <summary>Releases an online refund reservation the gateway refused (ADR-017).</summary>
    public void CancelRefund(decimal amount)
    {
        if (amount <= 0 || amount > RefundedAmount)
            throw new InvalidOperationException("Cannot release more than was reserved.");
        RefundedAmount -= amount;
    }
}
