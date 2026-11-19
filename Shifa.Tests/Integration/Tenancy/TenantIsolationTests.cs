using Microsoft.EntityFrameworkCore;
using Core.Entities.Clinic;
using Core.Entities.Common;
using Infrastructure.Data;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Tenancy;

/// <summary>
/// Proves the data-layer tenant isolation (ADR-002) against SQL Server, using Branch as the
/// tenant-owned example. Tenant 1 = "Al Noor Clinics", tenant 2 = "Al Shifa Medical Center".
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class TenantIsolationTests : IAsyncLifetime
{
    private const long AlNoor = 1;
    private const long AlShifa = 2;

    private readonly SqlServerFixture _db;

    public TenantIsolationTests(SqlServerFixture db)
    {
        _db = db;
    }

    public async ValueTask InitializeAsync() => await _db.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_tenant_sees_only_its_own_branches()
    {
        await AddBranchAsync(AlNoor, "MAIN", "Al Noor - Nasr City");
        await AddBranchAsync(AlShifa, "MAIN", "Al Shifa - Maadi");

        await using var alNoor = _db.CreateClinicContext(AlNoor);
        var names = await alNoor.Branches.Select(b => b.Name).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Al Noor - Nasr City"], names);
    }

    [Fact]
    public async Task Another_tenants_branch_is_not_found_by_id()
    {
        var alShifaBranchId = await AddBranchAsync(AlShifa, "MAIN", "Al Shifa - Maadi");

        await using var alNoor = _db.CreateClinicContext(AlNoor);
        var branch = await alNoor.Branches.FirstOrDefaultAsync(b => b.Id == alShifaBranchId, TestContext.Current.CancellationToken);

        Assert.Null(branch);
    }

    [Fact]
    public async Task Without_a_tenant_context_no_tenant_rows_are_returned()
    {
        await AddBranchAsync(AlNoor, "MAIN", "Al Noor - Nasr City");

        await using var anonymous = _db.CreateClinicContext(tenantId: null);

        Assert.Empty(await anonymous.Branches.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Insert_stamps_the_tenant_from_the_context()
    {
        var id = await AddBranchAsync(AlNoor, "MAIN", "Al Noor - Nasr City");

        await using var platform = _db.CreateClinicContext(tenantId: null);
        var stored = await platform.Branches.IgnoreQueryFilters([ModuleDbContext.TenantFilter])
            .SingleAsync(b => b.Id == id, TestContext.Current.CancellationToken);

        Assert.Equal(AlNoor, stored.TenantId);
        Assert.Equal(1, stored.CreatedBy);
        Assert.NotEqual(default, stored.CreationDate);
    }

    [Fact]
    public async Task Inserting_a_row_for_another_tenant_is_rejected()
    {
        await using var alNoor = _db.CreateClinicContext(AlNoor);
        var branch = NewBranch("MAIN", "Injected");
        await alNoor.Branches.AddAsync(branch, TestContext.Current.CancellationToken);
        alNoor.Entry(branch).Property(nameof(TenantOwnedEntity.TenantId)).CurrentValue = AlShifa;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => alNoor.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Contains("another tenant", error.Message);
    }

    [Fact]
    public async Task Updating_another_tenants_row_is_rejected()
    {
        var alShifaBranchId = await AddBranchAsync(AlShifa, "MAIN", "Al Shifa - Maadi");

        // Simulates code that got hold of another tenant's entity (e.g. through IgnoreQueryFilters
        // or a cache) and attaches it to a context acting for Al Noor.
        Branch foreign;
        await using (var platform = _db.CreateClinicContext(tenantId: null))
        {
            foreign = await platform.Branches.IgnoreQueryFilters([ModuleDbContext.TenantFilter]).AsNoTracking()
                .SingleAsync(b => b.Id == alShifaBranchId, TestContext.Current.CancellationToken);
        }

        await using var alNoor = _db.CreateClinicContext(AlNoor);
        alNoor.Branches.Attach(foreign);
        foreign.Name = "Hijacked";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => alNoor.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Contains("another tenant", error.Message);
    }

    [Fact]
    public async Task Changing_the_tenant_of_a_row_is_rejected()
    {
        var id = await AddBranchAsync(AlNoor, "MAIN", "Al Noor - Nasr City");

        await using var alNoor = _db.CreateClinicContext(AlNoor);
        var branch = await alNoor.Branches.SingleAsync(b => b.Id == id, TestContext.Current.CancellationToken);

        // TenantId is part of the (TenantId, Id) key, so EF Core refuses the change immediately,
        // before SaveChanges. AuditTenantInterceptor keeps its own check as a second line of defense.
        var error = Assert.Throws<InvalidOperationException>(() =>
            alNoor.Entry(branch).Property(nameof(TenantOwnedEntity.TenantId)).CurrentValue = AlShifa);
        Assert.Contains("part of a key", error.Message);
    }

    [Fact]
    public async Task Saving_tenant_data_without_a_tenant_context_is_rejected()
    {
        await using var anonymous = _db.CreateClinicContext(tenantId: null);
        await anonymous.Branches.AddAsync(NewBranch("MAIN", "No tenant"), TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => anonymous.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Contains("no tenant context", error.Message);
    }

    [Fact]
    public async Task The_same_code_is_allowed_in_two_tenants_but_not_twice_in_one()
    {
        await AddBranchAsync(AlNoor, "MAIN", "Al Noor - Nasr City");
        await AddBranchAsync(AlShifa, "MAIN", "Al Shifa - Maadi");

        await Assert.ThrowsAsync<DbUpdateException>(() => AddBranchAsync(AlNoor, "MAIN", "Al Noor - Heliopolis"));
    }

    [Fact]
    public async Task HiLo_assigns_the_id_before_SaveChanges()
    {
        await using var alNoor = _db.CreateClinicContext(AlNoor);
        var branch = NewBranch("MAIN", "Al Noor - Nasr City");

        await alNoor.Branches.AddAsync(branch, TestContext.Current.CancellationToken);

        Assert.True(branch.Id > 0);
    }

    [Fact]
    public async Task Remove_soft_deletes_and_hides_the_row()
    {
        var id = await AddBranchAsync(AlNoor, "MAIN", "Al Noor - Nasr City");

        await using (var alNoor = _db.CreateClinicContext(AlNoor))
        {
            var branch = await alNoor.Branches.SingleAsync(b => b.Id == id, TestContext.Current.CancellationToken);
            alNoor.Branches.Remove(branch);
            await alNoor.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var check = _db.CreateClinicContext(AlNoor);
        Assert.False(await check.Branches.AnyAsync(b => b.Id == id, TestContext.Current.CancellationToken));

        var stored = await check.Branches.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .SingleAsync(b => b.Id == id, TestContext.Current.CancellationToken);
        Assert.True(stored.IsDeleted);
    }

    private async Task<long> AddBranchAsync(long tenantId, string code, string name)
    {
        await using var context = _db.CreateClinicContext(tenantId);
        var branch = NewBranch(code, name);
        await context.Branches.AddAsync(branch, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return branch.Id;
    }

    private static Branch NewBranch(string code, string name) => new() { Code = code, Name = name };
}
