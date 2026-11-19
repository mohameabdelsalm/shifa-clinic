using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Infrastructure.Data;
using Core.Interfaces.Common;
using Infrastructure.Common.TenantManager;
using Infrastructure.Data.Interceptors;

namespace Shifa.Tests.Integration.Infrastructure;

/// <summary>
/// One SQL Server test database for the whole test run.
/// It applies every module's migrations once (creating Shifa_Tests if it doesn't exist) and
/// empties the module tables between tests with Respawn. It never drops the database.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string DefaultConnectionString =
        "Server=.;Database=Shifa_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly string[] ModuleSchemas =
    [
        TenancyDbContext.SchemaName, ClinicDbContext.SchemaName, IdentityDbContext.SchemaName, PatientsDbContext.SchemaName, SchedulingDbContext.SchemaName,
        ClinicalDbContext.SchemaName, BillingDbContext.SchemaName, NotificationsDbContext.SchemaName,
    ];

    private Respawner? _respawner;

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("SHIFA_TEST_CONNECTION") ?? DefaultConnectionString;

    public async ValueTask InitializeAsync()
    {
        await using (var tenancy = CreateTenancyContext())
            await tenancy.Database.MigrateAsync();

        await using (var clinic = CreateClinicContext(tenantId: null))
            await clinic.Database.MigrateAsync();

        await using (var identity = CreateIdentityContext())
            await identity.Database.MigrateAsync();

        await using (var patients = CreatePatientsContext(tenantId: null))
            await patients.Database.MigrateAsync();

        await using (var scheduling = CreateSchedulingContext(tenantId: null))
            await scheduling.Database.MigrateAsync();

        // Modules added after Phase 5 (clinical, billing, notifications).
        await using (var clinical = Create<ClinicalDbContext>(ClinicalDbContext.SchemaName, null, (o, t) => new ClinicalDbContext(o, t)))
            await clinical.Database.MigrateAsync();
        await using (var billing = CreateBillingContext(tenantId: null))
            await billing.Database.MigrateAsync();
        await using (var notifications = Create<NotificationsDbContext>(NotificationsDbContext.SchemaName, null, (o, t) => new NotificationsDbContext(o, t)))
            await notifications.Database.MigrateAsync();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = ModuleSchemas,
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    /// <summary>Runs a COUNT(*) query on the test database (test-written SQL only).</summary>
    public async Task<int> CountAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public BillingDbContext CreateBillingContext(long? tenantId) =>
        Create<BillingDbContext>(BillingDbContext.SchemaName, tenantId, (o, t) => new BillingDbContext(o, t));

    private TContext Create<TContext>(string schema, long? tenantId, Func<DbContextOptions<TContext>, ITenantContext, TContext> factory)
        where TContext : ModuleDbContext =>
        factory(BuildOptions<TContext>(schema, CreateTenantContext(tenantId), out var tenantContext), tenantContext);

    public SchedulingDbContext CreateSchedulingContext(long? tenantId) =>
        new(BuildOptions<SchedulingDbContext>(SchedulingDbContext.SchemaName, CreateTenantContext(tenantId), out var tenantContext), tenantContext);

    public PatientsDbContext CreatePatientsContext(long? tenantId) =>
        new(BuildOptions<PatientsDbContext>(PatientsDbContext.SchemaName, CreateTenantContext(tenantId), out var tenantContext), tenantContext);

    /// <summary>A ClinicDbContext acting for <paramref name="tenantId"/> (null = no tenant context).</summary>
    public ClinicDbContext CreateClinicContext(long? tenantId) =>
        new(BuildOptions<ClinicDbContext>(ClinicDbContext.SchemaName, CreateTenantContext(tenantId), out var tenantContext), tenantContext);

    public IdentityDbContext CreateIdentityContext() =>
        new(BuildOptions<IdentityDbContext>(IdentityDbContext.SchemaName, new TenantContext(), out var tenantContext), tenantContext);

    public TenancyDbContext CreateTenancyContext() =>
        new(BuildOptions<TenancyDbContext>(TenancyDbContext.SchemaName, new TenantContext(), out var tenantContext), tenantContext);

    private static TenantContext CreateTenantContext(long? tenantId)
    {
        var tenantContext = new TenantContext();
        if (tenantId is not null)
            tenantContext.SetTenant(tenantId.Value);
        return tenantContext;
    }

    private DbContextOptions<TContext> BuildOptions<TContext>(string schema, TenantContext tenant, out ITenantContext tenantContext)
        where TContext : ModuleDbContext
    {
        tenantContext = tenant;
        return new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .AddInterceptors(new AuditTenantInterceptor(tenant, new TestCurrentUser(), TimeProvider.System))
            .Options;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public long? GetUserId() => 1;
        public string GetUserName() => "integration-test";
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
