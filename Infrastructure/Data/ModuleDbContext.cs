using System.Reflection;
using System.Text.Json;
using Core.Entities.Common;
using Core.Events;
using Core.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Base class of every module's DbContext. It applies the rules every module must follow:
/// <list type="bullet">
/// <item>the module's own SQL Server schema and HiLo sequence (ADR-003);</item>
/// <item>the <c>Tenant</c> query filter and a unique (TenantId, Id) key on tenant-owned entities (ADR-002);</item>
/// <item>the <c>SoftDelete</c> query filter on every entity;</item>
/// <item>decimal(18,2) for money and enums stored as text.</item>
/// </list>
/// Don't use DbContext pooling with this class: the tenant comes from the scoped ITenantContext.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    public const string TenantFilter = "Tenant";
    public const string SoftDeleteFilter = "SoftDelete";

    private static readonly MethodInfo ConfigureTenantOwnedMethod =
        typeof(ModuleDbContext).GetMethod(nameof(ConfigureTenantOwned), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ConfigurePlatformOwnedMethod =
        typeof(ModuleDbContext).GetMethod(nameof(ConfigurePlatformOwned), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly ITenantContext _tenantContext;

    protected ModuleDbContext(DbContextOptions options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>The SQL Server schema owned by the module, e.g. "clinic".</summary>
    public abstract string Schema { get; }

    /// <summary>
    /// The module folder under Infrastructure/Configurations, e.g. "Clinic". All DbContexts live in one assembly,
    /// so each one applies only the entity configurations of its own module.
    /// </summary>
    protected abstract string Module { get; }

    /// <summary>Read by the Tenant filter on every query execution (not cached with the model).</summary>
    private long? CurrentTenantId => _tenantContext.TenantId;

    /// <summary>The module publishes integration events: it gets an OutboxMessages table (ADR-005).</summary>
    public virtual bool HasOutbox => false;

    /// <summary>The module consumes integration events: it gets an InboxMessages table (ADR-005).</summary>
    public virtual bool HasInbox => false;

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    /// <summary>
    /// Adds an integration event to this module's outbox; it is saved by the next SaveChanges, together with the
    /// business change (never published for a change that wasn't saved). The event's tenant is the current tenant.
    /// </summary>
    public void AddEvent(IIntegrationEvent integrationEvent, DateTimeOffset now)
    {
        if (!HasOutbox)
            throw new InvalidOperationException($"{GetType().Name} has no outbox.");

        OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("Integration events need a tenant context."),
            Type = integrationEvent.GetType().Name,
            PayloadJson = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            OccurredAt = now,
            NextAttemptAt = now,
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(50);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        // The sequence is created in the default schema above. Passing the schema again here
        // makes EF report pending model changes on every run.
        modelBuilder.UseHiLo("EntityFrameworkHiLoSequence");
        var configurationsNamespace = $"Infrastructure.Configurations.{Module}";
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly, type => type.Namespace == configurationsNamespace);

        // The OutboxMessages/InboxMessages DbSets live on this base class, so EF would add both tables to every module:
        // keep only what the module opted into.
        if (!HasOutbox)
            modelBuilder.Ignore<OutboxMessage>();
        if (!HasInbox)
            modelBuilder.Ignore<InboxMessage>();

        if (HasOutbox)
        {
            modelBuilder.Entity<OutboxMessage>(outbox =>
            {
                outbox.ToTable("OutboxMessages");
                outbox.HasKey(m => m.Id);
                outbox.Property(m => m.Id).ValueGeneratedNever();
                outbox.Property(m => m.Type).HasMaxLength(200).IsRequired();
                outbox.Property(m => m.PayloadJson).IsRequired();
                outbox.Property(m => m.LastError).HasMaxLength(1000);
                // Fast polling of what is still to do (database-design.md).
                outbox.HasIndex(m => new { m.NextAttemptAt, m.OccurredAt })
                    .HasFilter("[ProcessedAt] IS NULL AND [PoisonedAt] IS NULL")
                    .HasDatabaseName("IX_OutboxMessages_Pending");
                outbox.HasIndex(m => m.ProcessedAt);
            });
        }

        if (HasInbox)
        {
            modelBuilder.Entity<InboxMessage>(inbox =>
            {
                inbox.ToTable("InboxMessages");
                inbox.HasKey(m => new { m.MessageId, m.Consumer });
                inbox.Property(m => m.Consumer).HasMaxLength(200);
                inbox.HasIndex(m => m.ProcessedAt);
            });
        }

        var rootEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => entityType.BaseType is null && !entityType.IsOwned())
            .Select(entityType => entityType.ClrType)
            .ToList();

        foreach (var clrType in rootEntityTypes)
        {
            if (typeof(TenantOwnedEntity).IsAssignableFrom(clrType))
                ConfigureTenantOwnedMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
            else if (typeof(BaseModel).IsAssignableFrom(clrType))
                ConfigurePlatformOwnedMethod.MakeGenericMethod(clrType).Invoke(null, [modelBuilder]);
        }
    }

    private void ConfigureTenantOwned<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantOwnedEntity
    {
        var entity = modelBuilder.Entity<TEntity>();

        // Target of composite foreign keys: children reference (TenantId, Id), so the database
        // rejects a child row that points to another tenant's parent.
        entity.HasAlternateKey(e => new { e.TenantId, e.Id });

        // No tenant in the context → no rows. Never "all rows".
        entity.HasQueryFilter(TenantFilter, e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        entity.HasQueryFilter(SoftDeleteFilter, e => !e.IsDeleted);
    }

    private static void ConfigurePlatformOwned<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseModel
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(SoftDeleteFilter, e => !e.IsDeleted);
    }
}
