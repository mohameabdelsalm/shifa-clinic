namespace Core.DTO.Patients;

public sealed record PortalPatientAccess(long PatientId, string Relationship, bool CanBook);
