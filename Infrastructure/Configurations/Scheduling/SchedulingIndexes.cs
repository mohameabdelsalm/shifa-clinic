namespace Infrastructure.Configurations.Scheduling;

/// <summary>Index names other code relies on (unique-violation mapping).</summary>
public static class SchedulingIndexes
{
    public const string SlotTick = "IX_AppointmentSlots_Tick";
    public const string PatientDoctorDay = "IX_Appointments_PatientDoctorDay";
    public const string QueueNumber = "IX_Appointments_Queue";
    public const string Idempotency = "IX_IdempotencyRecords_Key";
}
