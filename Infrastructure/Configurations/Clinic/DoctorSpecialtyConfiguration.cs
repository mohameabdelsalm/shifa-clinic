using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
    {
        builder.References<DoctorSpecialty, Doctor>(x => x.DoctorId);
        builder.References<DoctorSpecialty, Specialty>(x => x.SpecialtyId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorId, x.SpecialtyId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
