using Core.Entities.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Scheduling;

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

        // Statistics and reports count by local day without a doctor (ADR-017): covering index, no lookups.
        builder.HasIndex(a => new { a.TenantId, a.LocalDate })
            .IncludeProperties(a => new { a.BranchId, a.DoctorId, a.Status, a.StartAt, a.EndAt })
            .HasDatabaseName("IX_Appointments_TenantId_LocalDate_Stats");
        // The no-show job looks across clinics for confirmed appointments past their start.
        builder.HasIndex(a => new { a.Status, a.StartAt });
    }
}
