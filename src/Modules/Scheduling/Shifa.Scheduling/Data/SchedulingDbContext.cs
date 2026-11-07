using Microsoft.EntityFrameworkCore;
using Shifa.Core.Interfaces.Common;
using Shifa.Infrastructure.Data;
using Shifa.Scheduling.Entities;

namespace Shifa.Scheduling.Data;

public class SchedulingDbContext : ModuleDbContext
{
    public const string SchemaName = "scheduling";

    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistory => Set<AppointmentStatusHistory>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
}
