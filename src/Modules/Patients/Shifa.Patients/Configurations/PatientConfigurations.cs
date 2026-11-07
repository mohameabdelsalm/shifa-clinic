using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shifa.Patients.Entities;

namespace Shifa.Patients.Configurations;

internal static class TenantForeignKey
{
    /// <summary>Composite foreign key (TenantId, PatientId) -> (TenantId, Id): a child can't point to another tenant's patient.</summary>
    public static void ReferencesPatient<TChild>(this EntityTypeBuilder<TChild> builder) where TChild : class =>
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey("TenantId", "PatientId")
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
}

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

public class PatientConsentConfiguration : IEntityTypeConfiguration<PatientConsent>
{
    public void Configure(EntityTypeBuilder<PatientConsent> builder)
    {
        builder.Ignore(c => c.IsActive);
        builder.ReferencesPatient();
        builder.HasIndex(c => new { c.TenantId, c.PatientId, c.ConsentType });
    }
}
