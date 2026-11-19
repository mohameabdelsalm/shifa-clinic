using Microsoft.EntityFrameworkCore;
using Shifa.Tests.Integration.Infrastructure;
using Core.Entities.Tenancy;
using Core.Enums.Tenancy;

namespace Shifa.Tests.Integration.Tenancy;

[Collection(SqlServerCollection.Name)]
public sealed class PlatformOwnedEntityTests : IAsyncLifetime
{
    private readonly SqlServerFixture _db;

    public PlatformOwnedEntityTests(SqlServerFixture db)
    {
        _db = db;
    }

    public async ValueTask InitializeAsync() => await _db.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Platform_owned_entities_are_saved_without_a_tenant_context()
    {
        await using (var tenancy = _db.CreateTenancyContext())
        {
            await tenancy.Tenants.AddAsync(new Tenant { Name = "Al Noor Clinics", Slug = "al-noor" }, TestContext.Current.CancellationToken);
            await tenancy.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var check = _db.CreateTenancyContext();
        var tenant = await check.Tenants.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal("al-noor", tenant.Slug);
        Assert.Equal(TenantStatusEnum.Trial, tenant.Status);
    }

    [Fact]
    public async Task Tenant_slug_is_unique()
    {
        await using var tenancy = _db.CreateTenancyContext();
        await tenancy.Tenants.AddAsync(new Tenant { Name = "Al Noor Clinics", Slug = "al-noor" }, TestContext.Current.CancellationToken);
        await tenancy.Tenants.AddAsync(new Tenant { Name = "Al Noor Again", Slug = "al-noor" }, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => tenancy.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
