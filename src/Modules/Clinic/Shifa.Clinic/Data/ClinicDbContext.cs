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
}
