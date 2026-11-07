using Microsoft.EntityFrameworkCore;
using Shifa.Clinic.Entities;
using Shifa.Core.Interfaces.Common;
using Shifa.Infrastructure.Data;

namespace Shifa.Clinic.Data;

public class ClinicDbContext : ModuleDbContext
{
    public const string SchemaName = "clinic";

    public ClinicDbContext(DbContextOptions<ClinicDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSpecialty> DoctorSpecialties => Set<DoctorSpecialty>();
    public DbSet<DoctorBranch> DoctorBranches => Set<DoctorBranch>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<DoctorTimeOff> DoctorTimeOffs => Set<DoctorTimeOff>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
}
