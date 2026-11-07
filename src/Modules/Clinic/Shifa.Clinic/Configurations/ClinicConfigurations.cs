using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shifa.Clinic.Entities;

namespace Shifa.Clinic.Configurations;

internal static class TenantForeignKey
{
    /// <summary>
    /// Composite foreign key (TenantId, XId) -> (TenantId, Id): the database rejects a row that points
    /// to another tenant's parent. Every relationship between tenant-owned entities must use it.
    /// </summary>
    public static void References<TChild, TParent>(
        this EntityTypeBuilder<TChild> builder,
        System.Linq.Expressions.Expression<Func<TChild, object?>> foreignKey)
        where TChild : class
        where TParent : class
    {
        // Built by hand because EF needs the (TenantId, XId) pair in the same order as the principal key.
        var fkProperty = ((System.Linq.Expressions.MemberExpression)(foreignKey.Body is System.Linq.Expressions.UnaryExpression u ? u.Operand : foreignKey.Body)).Member.Name;
        builder.HasOne<TParent>().WithMany()
            .HasForeignKey("TenantId", fkProperty)
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.Property(s => s.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(s => s.NameAr).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => new { s.TenantId, s.NameEn }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.Property(d => d.FullNameEn).HasMaxLength(200).IsRequired();
        builder.Property(d => d.FullNameAr).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(100);
    }
}

public class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
    {
        builder.References<DoctorSpecialty, Doctor>(x => x.DoctorId);
        builder.References<DoctorSpecialty, Specialty>(x => x.SpecialtyId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorId, x.SpecialtyId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class DoctorBranchConfiguration : IEntityTypeConfiguration<DoctorBranch>
{
    public void Configure(EntityTypeBuilder<DoctorBranch> builder)
    {
        builder.References<DoctorBranch, Doctor>(x => x.DoctorId);
        builder.References<DoctorBranch, Branch>(x => x.BranchId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorId, x.BranchId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.TenantId, x.BranchId });
    }
}

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.References<DoctorSchedule, DoctorBranch>(x => x.DoctorBranchId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorBranchId, x.DayOfWeek });
    }
}

public class DoctorTimeOffConfiguration : IEntityTypeConfiguration<DoctorTimeOff>
{
    public void Configure(EntityTypeBuilder<DoctorTimeOff> builder)
    {
        builder.Property(t => t.Reason).HasMaxLength(500);
        builder.References<DoctorTimeOff, Doctor>(x => x.DoctorId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorId, x.FromAt });
    }
}

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

public class ServicePriceConfiguration : IEntityTypeConfiguration<ServicePrice>
{
    public void Configure(EntityTypeBuilder<ServicePrice> builder)
    {
        builder.References<ServicePrice, Service>(x => x.ServiceId);
        builder.References<ServicePrice, Branch>(x => x.BranchId);

        // At most one open (current) price per service and branch.
        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.BranchId }).IsUnique()
            .HasFilter("[EffectiveTo] IS NULL AND [IsDeleted] = 0");
    }
}
