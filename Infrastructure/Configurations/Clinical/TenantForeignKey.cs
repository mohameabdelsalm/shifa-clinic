using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

internal static class TenantForeignKey
{
    /// <summary>Composite foreign key (TenantId, VisitId) -> (TenantId, Id).</summary>
    public static void ReferencesVisit<TChild>(this EntityTypeBuilder<TChild> builder) where TChild : class =>
        builder.HasOne<Visit>().WithMany()
            .HasForeignKey("TenantId", "VisitId")
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
}
