using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.Ignore(v => v.IsOpen);
        builder.Property(v => v.RowVersion).IsRowVersion();

        // One visit per appointment, also when two open-visit requests race.
        builder.HasIndex(v => new { v.TenantId, v.AppointmentId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(ClinicalIndexes.VisitAppointment);

        builder.HasIndex(v => new { v.TenantId, v.PatientId, v.OpenedAt });
        builder.HasIndex(v => new { v.TenantId, v.BranchId, v.OpenedAt });
        builder.HasIndex(v => new { v.TenantId, v.DoctorId, v.OpenedAt });
    }
}
