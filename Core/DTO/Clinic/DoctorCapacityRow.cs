namespace Core.DTO.Clinic;

/// <summary>A doctor's scheduled working minutes at one branch over a period, minus time off.</summary>
public sealed record DoctorCapacityRow(long DoctorId, long BranchId, int Minutes);
