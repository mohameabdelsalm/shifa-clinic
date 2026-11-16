namespace Core.Enums.Billing;

/// <summary>Follows from the amounts (ADR-011).</summary>
public enum InvoiceStatusEnum
{
    Open = 1,
    PartiallyPaid,

    /// <summary>Balance 0, including fully credited.</summary>
    Paid,
    Void,
}
