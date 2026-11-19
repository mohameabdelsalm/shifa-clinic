namespace Core.Models.Billing;

/// <summary>A service from the branch price list; the price is taken from the list (no typed prices).</summary>
public class InvoiceItemModel
{
    public long ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
}
