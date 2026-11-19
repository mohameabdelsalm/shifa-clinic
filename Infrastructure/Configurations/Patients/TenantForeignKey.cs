using Core.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Patients;

internal static class TenantForeignKey
{
    /// <summary>Composite foreign key (TenantId, PatientId) -> (TenantId, Id): a child can't point to another tenant's patient.</summary>
    public static void ReferencesPatient<TChild>(this EntityTypeBuilder<TChild> builder) where TChild : class =>
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey("TenantId", "PatientId")
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
}
