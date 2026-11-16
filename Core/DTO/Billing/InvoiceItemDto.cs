namespace Core.DTO.Billing;

public class InvoiceItemDto
{
    public long ServiceId { get; set; }
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
