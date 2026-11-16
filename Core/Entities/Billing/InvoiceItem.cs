using Core.Entities.Common;

namespace Core.Entities.Billing;

/// <summary>One service on an invoice, with the price at the time (snapshot, rule 8).</summary>
public class InvoiceItem : TenantOwnedEntity
{
    public long InvoiceId { get; set; }
    public long ServiceId { get; set; }
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
