using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.Property(d => d.FullNameEn).HasMaxLength(200).IsRequired();
        builder.Property(d => d.FullNameAr).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(100);
    }
}
