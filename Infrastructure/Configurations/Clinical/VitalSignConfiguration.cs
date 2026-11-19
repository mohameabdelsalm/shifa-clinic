using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class VitalSignConfiguration : IEntityTypeConfiguration<VitalSign>
{
    public void Configure(EntityTypeBuilder<VitalSign> builder)
    {
        builder.Property(v => v.TemperatureC).HasPrecision(4, 1);
        builder.Property(v => v.WeightKg).HasPrecision(5, 1);
        builder.Property(v => v.HeightCm).HasPrecision(5, 1);
        builder.ReferencesVisit();
    }
}
