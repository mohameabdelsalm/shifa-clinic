namespace Core.Models.Billing;

/// <summary>An extra invoice for additional services (the visit invoice is created at check-in).</summary>
public class CreateInvoiceModel
{
    public long PatientId { get; set; }
    public long BranchId { get; set; }
    public long? AppointmentId { get; set; }
    public List<InvoiceItemModel> Items { get; set; } = [];
}
