namespace Core.DTO.Billing;

public class InvoiceDto : InvoiceListDto
{
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public string? VoidReason { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = [];
    public List<PaymentDto> Payments { get; set; } = [];
    public List<RefundDto> Refunds { get; set; } = [];
    public List<CreditNoteDto> CreditNotes { get; set; } = [];
}
