using Core.Entities.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Scheduling;

internal static class TenantForeignKey
{
    /// <summary>Composite foreign key (TenantId, AppointmentId) -> (TenantId, Id).</summary>
    public static void ReferencesAppointment<TChild>(this EntityTypeBuilder<TChild> builder) where TChild : class =>
        builder.HasOne<Appointment>().WithMany()
            .HasForeignKey("TenantId", "AppointmentId")
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
}
