using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

internal static class TenantForeignKey
{
    /// <summary>Composite foreign key (TenantId, foreignKey) -> (TenantId, Id) of TPrincipal.</summary>
    public static void References<TChild, TPrincipal>(this EntityTypeBuilder<TChild> builder, string foreignKey)
        where TChild : class where TPrincipal : class =>
        builder.HasOne<TPrincipal>().WithMany()
            .HasForeignKey("TenantId", foreignKey)
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
}
