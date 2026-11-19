using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class DoctorTimeOffConfiguration : IEntityTypeConfiguration<DoctorTimeOff>
{
    public void Configure(EntityTypeBuilder<DoctorTimeOff> builder)
    {
        builder.Property(t => t.Reason).HasMaxLength(500);
        builder.References<DoctorTimeOff, Doctor>(x => x.DoctorId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorId, x.FromAt });
    }
}
