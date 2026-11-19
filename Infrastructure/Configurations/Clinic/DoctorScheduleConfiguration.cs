using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.References<DoctorSchedule, DoctorBranch>(x => x.DoctorBranchId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorBranchId, x.DayOfWeek });
    }
}
