using Core.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Patients;

public class PatientIdentifierConfiguration : IEntityTypeConfiguration<PatientIdentifier>
{
    public void Configure(EntityTypeBuilder<PatientIdentifier> builder)
    {
        builder.Property(i => i.Value).HasMaxLength(50).IsRequired();
        builder.ReferencesPatient();

        // The same national ID or passport can't belong to two patients of a tenant (duplicate detection).
        builder.HasIndex(i => new { i.TenantId, i.Type, i.Value }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
