using Core.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Patients;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.Property(p => p.Mrn).HasMaxLength(20).IsRequired();
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.Address).HasMaxLength(500);

        // Not filtered by IsDeleted: a number or MRN is never reused, even if a record is ever removed.
        builder.HasIndex(p => new { p.TenantId, p.Number }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Mrn }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.PhoneNumber });
        builder.HasIndex(p => new { p.TenantId, p.FullName });
    }
}
