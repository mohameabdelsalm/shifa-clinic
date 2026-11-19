using Core.Entities.Patients;
using Core.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class PatientsDbContext : ModuleDbContext
{
    public const string SchemaName = "patients";

    public PatientsDbContext(DbContextOptions<PatientsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    protected override string Module => "Patients";

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientIdentifier> PatientIdentifiers => Set<PatientIdentifier>();
    public DbSet<PatientGuardian> PatientGuardians => Set<PatientGuardian>();
    public DbSet<PatientAccountAccess> PatientAccountAccesses => Set<PatientAccountAccess>();
    public DbSet<PatientConsent> PatientConsents => Set<PatientConsent>();
}
