using Core.Entities.Scheduling;
using Core.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class SchedulingDbContext : ModuleDbContext
{
    public const string SchemaName = "scheduling";

    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    protected override string Module => "Scheduling";

    public override bool HasOutbox => true;

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistory => Set<AppointmentStatusHistory>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
}
