using Core.Entities.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Scheduling;

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
