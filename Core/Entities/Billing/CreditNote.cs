using Core.Entities.Common;

namespace Core.Entities.Billing;

/// <summary>Reduces what is owed on an invoice (e.g. a service not given).</summary>
public class CreditNote : TenantOwnedEntity
{
    public long InvoiceId { get; set; }

    /// <summary>CN-yyyy-000001.</summary>
    public string Number { get; set; } = "";
    public int Year { get; set; }
    public int Sequence { get; set; }

    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public long CreatedByUserId { get; set; }
}
