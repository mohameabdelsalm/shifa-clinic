namespace Core.Enums.Billing;

public enum RefundStatusEnum
{
    /// <summary>Money returned (cash/card/wallet at the desk, or confirmed by the gateway). Counts in totals.</summary>
    Completed = 1,

    /// <summary>
    /// Online refund reserved before calling the gateway, or the gateway's answer is unknown (timeout, pending).
    /// The amount is held on the payment (no second refund of it), but not yet counted on the invoice.
    /// </summary>
    Pending,

    /// <summary>The gateway refused it; the reservation was released.</summary>
    Failed,
}
