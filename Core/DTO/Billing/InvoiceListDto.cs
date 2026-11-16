namespace Core.DTO.Billing;

public class InvoiceListDto
{
    public long Id { get; set; }
    public string Number { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Status { get; set; } = "";
    public long BranchId { get; set; }
    public long PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CreditedAmount { get; set; }
    public decimal Balance { get; set; }
}
