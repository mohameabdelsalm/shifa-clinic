using Core.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Patients;

public class PatientAccountAccessConfiguration : IEntityTypeConfiguration<PatientAccountAccess>
{
    public void Configure(EntityTypeBuilder<PatientAccountAccess> builder)
    {
        builder.Property(a => a.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.ReferencesPatient();
        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.PhoneNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(a => new { a.TenantId, a.PhoneNumber });
    }
}
