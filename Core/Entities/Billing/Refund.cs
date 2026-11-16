using Core.Entities.Common;
using Core.Enums.Billing;

namespace Core.Entities.Billing;

/// <summary>Money given back from one payment, by the same method.</summary>
public class Refund : TenantOwnedEntity
{
    public long PaymentId { get; set; }

    /// <summary>The refunder's shift (cash leaves that drawer); null for online refunds.</summary>
    public long? CashierShiftId { get; set; }

    /// <summary>RFD-yyyy-000001.</summary>
    public string Number { get; set; } = "";
    public int Year { get; set; }
    public int Sequence { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethodEnum Method { get; set; }
    public string Reason { get; set; } = "";
    public long RefundedByUserId { get; set; }

    /// <summary>The gateway's refund transaction id for online refunds.</summary>
    public string? ExternalReference { get; set; }
}
