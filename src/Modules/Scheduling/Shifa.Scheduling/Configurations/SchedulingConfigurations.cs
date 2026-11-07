using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shifa.Scheduling.Entities;

namespace Shifa.Scheduling.Configurations;

/// <summary>Index names other code relies on (unique-violation mapping).</summary>
public static class SchedulingIndexes
{
    public const string SlotTick = "IX_AppointmentSlots_Tick";
    public const string PatientDoctorDay = "IX_Appointments_PatientDoctorDay";
    public const string QueueNumber = "IX_Appointments_Queue";
    public const string Idempotency = "IX_IdempotencyRecords_Key";
}

internal static class TenantForeignKey
{
    /// <summary>Composite foreign key (TenantId, AppointmentId) -> (TenantId, Id).</summary>
    public static void ReferencesAppointment<TChild>(this EntityTypeBuilder<TChild> builder) where TChild : class =>
        builder.HasOne<Appointment>().WithMany()
            .HasForeignKey("TenantId", "AppointmentId")
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Ignore(a => a.IsActive);
        builder.Property(a => a.RowVersion).IsRowVersion();
        builder.Property(a => a.CancelReason).HasMaxLength(500);

        // Rule 3: one non-cancelled appointment per patient, doctor and local day — also under concurrency.
        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.DoctorId, a.LocalDate })
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled' AND [IsDeleted] = 0")
            .HasDatabaseName(SchedulingIndexes.PatientDoctorDay);

        builder.HasIndex(a => new { a.TenantId, a.BranchId, a.DoctorId, a.LocalDate, a.QueueNumber })
            .IsUnique()
            .HasFilter("[QueueNumber] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName(SchedulingIndexes.QueueNumber);

        builder.HasIndex(a => new { a.TenantId, a.BranchId, a.StartAt });
        builder.HasIndex(a => new { a.TenantId, a.DoctorId, a.StartAt });
        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.StartAt });
    }
}

public class AppointmentSlotConfiguration : IEntityTypeConfiguration<AppointmentSlot>
{
    public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
    {
        builder.ReferencesAppointment();

        // ADR-008: the final double-booking guard. Live rows only, so cancelling (soft delete) frees the time.
        builder.HasIndex(s => new { s.TenantId, s.DoctorId, s.SlotStart })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(SchedulingIndexes.SlotTick);
    }
}

public class AppointmentStatusHistoryConfiguration : IEntityTypeConfiguration<AppointmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<AppointmentStatusHistory> builder)
    {
        builder.Property(h => h.Reason).HasMaxLength(500);
        builder.ReferencesAppointment();
    }
}

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.Property(r => r.Operation).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Key).HasMaxLength(100).IsRequired();
        builder.Property(r => r.RequestHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.Operation, r.Key })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(SchedulingIndexes.Idempotency);
    }
}
