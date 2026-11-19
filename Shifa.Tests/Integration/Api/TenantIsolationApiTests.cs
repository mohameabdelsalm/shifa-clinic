using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Core.DTO.Identity;
using Core.Interfaces.Identity;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>
/// Phase 2 exit criterion: Tenant A can't read, update, delete or otherwise reach Tenant B's data
/// through the API (roles and staff are the tenant-owned data that exists so far).
/// Al Noor = tenant A, Al Shifa = tenant B.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class TenantIsolationApiTests
{
    private readonly SqlServerFixture _db;

    public TenantIsolationApiTests(SqlServerFixture db)
    {
        _db = db;
    }

    private sealed record Setup(ApiTestHost Api, string AlNoor, string AlShifa, long AlNoorTenantId, long AlShifaTenantId);

    private async Task<Setup> TwoTenantsAsync()
    {
        var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        var a = await api.CreateTenantAsync(platform, planId, "al-noor");
        var b = await api.CreateTenantAsync(platform, planId, "al-shifa");
        return new Setup(api, await api.AdminTokenAsync("al-noor"), await api.AdminTokenAsync("al-shifa"), a, b);
    }

    [Fact]
    public async Task Each_tenant_sees_only_its_own_roles()
    {
        var s = await TwoTenantsAsync();
        await using var _ = s.Api;

        var created = await s.Api.PostAsync("/api/roles", s.AlNoor, new { name = "Al Noor Only", permissions = new[] { "Patients.Read" } });
        Assert.Equal(HttpStatusCode.OK, created.Status);

        var alNoorRoles = await s.Api.GetAsync("/api/roles", s.AlNoor);
        var alShifaRoles = await s.Api.GetAsync("/api/roles", s.AlShifa);

        Assert.Contains(alNoorRoles.Data.EnumerateArray(), r => r.GetProperty("name").GetString() == "Al Noor Only");
        Assert.DoesNotContain(alShifaRoles.Data.EnumerateArray(), r => r.GetProperty("name").GetString() == "Al Noor Only");
        Assert.Equal(6, alShifaRoles.Data.GetArrayLength());   // exactly its own default roles
    }

    [Fact]
    public async Task Tenant_B_cannot_read_update_or_delete_tenant_A_roles()
    {
        var s = await TwoTenantsAsync();
        await using var _ = s.Api;

        var roleId = (await s.Api.PostAsync("/api/roles", s.AlNoor, new { name = "Al Noor Only", permissions = new[] { "Patients.Read" } })).Data.GetInt64();

        var read = await s.Api.GetAsync($"/api/roles/{roleId}", s.AlShifa);
        var update = await s.Api.PutAsync("/api/roles", s.AlShifa, new { id = roleId, name = "Hijacked", permissions = new[] { "Patients.Read" } });
        var delete = await s.Api.DeleteAsync($"/api/roles/{roleId}", s.AlShifa);

        Assert.Equal(HttpStatusCode.NotFound, read.Status);
        Assert.Equal(HttpStatusCode.NotFound, update.Status);
        Assert.Equal(HttpStatusCode.NotFound, delete.Status);

        // ...and the role is untouched for its owner.
        var stillThere = await s.Api.GetAsync($"/api/roles/{roleId}", s.AlNoor);
        Assert.Equal(HttpStatusCode.OK, stillThere.Status);
        Assert.Equal("Al Noor Only", stillThere.Data.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Tenant_B_cannot_read_update_or_suspend_tenant_A_staff()
    {
        var s = await TwoTenantsAsync();
        await using var _ = s.Api;

        var roles = await s.Api.GetAsync("/api/roles", s.AlNoor);
        var doctor = roles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Doctor").GetProperty("id").GetInt64();
        var staffId = (await s.Api.PostAsync("/api/staff", s.AlNoor, new
        {
            fullName = "Dr. Synthetic",
            email = "doctor@al-noor.test",
            password = ApiTestHost.Password,
            roleIds = new[] { doctor },
            allBranches = true,
        })).Data.GetInt64();

        var read = await s.Api.GetAsync($"/api/staff/{staffId}", s.AlShifa);
        var update = await s.Api.PutAsync("/api/staff", s.AlShifa, new { id = staffId, roleIds = new[] { doctor }, allBranches = true });
        var suspend = await s.Api.PutAsync($"/api/staff/{staffId}/suspend", s.AlShifa);
        var search = await s.Api.PostAsync("/api/staff/search", s.AlShifa, new { pageNumber = 1, pageSize = 50 });

        Assert.Equal(HttpStatusCode.NotFound, read.Status);
        Assert.Equal(HttpStatusCode.NotFound, update.Status);
        Assert.Equal(HttpStatusCode.NotFound, suspend.Status);
        Assert.DoesNotContain(search.Data.GetProperty("items").EnumerateArray(), m => m.GetProperty("email").GetString() == "doctor@al-noor.test");
    }

    [Fact]
    public async Task Tenant_A_cannot_assign_tenant_B_roles_to_its_staff()
    {
        var s = await TwoTenantsAsync();
        await using var _ = s.Api;

        var alShifaRoles = await s.Api.GetAsync("/api/roles", s.AlShifa);
        var foreignRole = alShifaRoles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Doctor").GetProperty("id").GetInt64();

        var result = await s.Api.PostAsync("/api/staff", s.AlNoor, new
        {
            fullName = "Dr. Synthetic",
            email = "doctor@al-noor.test",
            password = ApiTestHost.Password,
            roleIds = new[] { foreignRole },
            allBranches = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
    }

    [Fact]
    public async Task Membership_lookup_never_returns_another_tenants_membership()
    {
        var s = await TwoTenantsAsync();
        await using var _ = s.Api;

        var alNoorStaff = (await s.Api.PostAsync("/api/staff/search", s.AlNoor, new { pageNumber = 1, pageSize = 10 })).Data.GetProperty("items")[0];
        var membershipId = alNoorStaff.GetProperty("id").GetInt64();
        var userId = alNoorStaff.GetProperty("userId").GetInt64();

        await using var scope = s.Api.Services.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityApi>();
        var ct = TestContext.Current.CancellationToken;

        Assert.NotNull(await identity.GetMembershipAccessAsync(s.AlNoorTenantId, membershipId, userId, ct));
        Assert.Null(await identity.GetMembershipAccessAsync(s.AlShifaTenantId, membershipId, userId, ct));
    }

    [Fact]
    public async Task A_suspended_tenant_is_locked_out_immediately_and_can_return()
    {
        var s = await TwoTenantsAsync();
        await using var _ = s.Api;
        var platform = await s.Api.PlatformTokenAsync();

        Assert.Equal(HttpStatusCode.OK, (await s.Api.GetAsync("/api/roles", s.AlNoor)).Status);

        Assert.Equal(HttpStatusCode.OK, (await s.Api.PutAsync($"/api/platform/tenants/{s.AlNoorTenantId}/suspend", platform)).Status);

        Assert.Equal(HttpStatusCode.Unauthorized, (await s.Api.GetAsync("/api/roles", s.AlNoor)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await s.Api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "al-noor")).Status);
        Assert.Equal(HttpStatusCode.OK, (await s.Api.GetAsync("/api/roles", s.AlShifa)).Status);   // other tenant unaffected

        // A stale token of the suspended tenant doesn't block signing in somewhere else.
        var relogin = await s.Api.CallAsync(HttpMethod.Post, "/api/auth/login", s.AlNoor,
            new { identifier = ApiTestHost.AdminEmail("al-shifa"), password = ApiTestHost.Password, tenantSlug = "al-shifa" });
        Assert.Equal(HttpStatusCode.OK, relogin.Status);

        Assert.Equal(HttpStatusCode.OK, (await s.Api.PutAsync($"/api/platform/tenants/{s.AlNoorTenantId}/activate", platform)).Status);
        Assert.Equal(HttpStatusCode.OK, (await s.Api.GetAsync("/api/roles", s.AlNoor)).Status);
    }
}
