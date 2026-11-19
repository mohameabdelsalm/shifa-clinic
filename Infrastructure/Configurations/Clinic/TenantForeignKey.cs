using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

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
