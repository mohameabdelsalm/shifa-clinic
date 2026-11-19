using Core.Entities.Clinical;
using Core.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ClinicalDbContext : ModuleDbContext
{
    public const string SchemaName = "clinical";

    public ClinicalDbContext(DbContextOptions<ClinicalDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    protected override string Module => "Clinical";

    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<VisitAttachment> VisitAttachments => Set<VisitAttachment>();
    public DbSet<ClinicalAddendum> ClinicalAddenda => Set<ClinicalAddendum>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
}
