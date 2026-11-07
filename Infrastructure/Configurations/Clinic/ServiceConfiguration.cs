using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.Property(s => s.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(s => s.NameAr).HasMaxLength(200).IsRequired();
        builder.References<Service, Specialty>(x => x.SpecialtyId);
        builder.HasIndex(s => new { s.TenantId, s.SpecialtyId, s.NameEn }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
