namespace Core.DTO.Patients;

/// <summary>The minimum a payment gateway needs as billing data (no medical or identity details).</summary>
public sealed record PatientBillingContact(string FullName, string? Email);
