namespace Core.DTO.Billing;

/// <summary>What Scheduling tells Billing at check-in to create the visit invoice (ADR-011).</summary>
public sealed record VisitInvoiceRequest(long AppointmentId, long PatientId, long BranchId, long ServiceId);
