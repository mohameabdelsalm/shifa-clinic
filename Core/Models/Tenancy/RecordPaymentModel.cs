namespace Core.Models.Tenancy;

public class RecordPaymentModel
{
    public long TenantId { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}
