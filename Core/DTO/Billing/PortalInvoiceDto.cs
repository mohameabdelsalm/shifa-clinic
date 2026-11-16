namespace Core.DTO.Billing;

/// <summary>An invoice as the patient sees it in the portal (no internal user, shift or reference fields).</summary>
public class PortalInvoiceDto
{
    public long Id { get; set; }
    public string Number { get; set; } = "";
    public long PatientId { get; set; }
    public long BranchId { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public string Status { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CreditedAmount { get; set; }
    public decimal Balance { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = [];
}
