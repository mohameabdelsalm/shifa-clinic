using Core.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Patients;

public class PatientGuardianConfiguration : IEntityTypeConfiguration<PatientGuardian>
{
    public void Configure(EntityTypeBuilder<PatientGuardian> builder)
    {
        builder.Property(g => g.FullName).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Relationship).HasMaxLength(50).IsRequired();
        builder.Property(g => g.PhoneNumber).HasMaxLength(20);
        builder.ReferencesPatient();
    }
}
