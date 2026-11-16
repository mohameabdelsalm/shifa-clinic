namespace Core.DTO.Billing;

public class CreditNoteDto
{
    public long Id { get; set; }
    public string Number { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
