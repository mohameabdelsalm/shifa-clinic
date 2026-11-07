using Core.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Patients;

public class PatientConsentConfiguration : IEntityTypeConfiguration<PatientConsent>
{
    public void Configure(EntityTypeBuilder<PatientConsent> builder)
    {
        builder.Ignore(c => c.IsActive);
        builder.ReferencesPatient();
        builder.HasIndex(c => new { c.TenantId, c.PatientId, c.ConsentType });
    }
}
