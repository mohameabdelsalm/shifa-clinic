namespace Core.Models.Billing;

public class AddCreditNoteModel
{
    public long InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
}
