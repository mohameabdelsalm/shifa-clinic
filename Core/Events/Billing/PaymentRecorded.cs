namespace Core.Events.Billing;

/// <summary>Money received for an invoice (desk or online). Used for the patient's receipt message.</summary>
public sealed record PaymentRecorded(long PaymentId, string ReceiptNumber, long InvoiceId, string InvoiceNumber, long PatientId, long BranchId,
    decimal Amount, string CurrencyCode, string Method) : IIntegrationEvent;
