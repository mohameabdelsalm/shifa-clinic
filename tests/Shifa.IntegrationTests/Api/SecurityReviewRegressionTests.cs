using System.Net;
using Microsoft.EntityFrameworkCore;
using Shifa.Core.Common;
using Shifa.IntegrationTests.Infrastructure;

namespace Shifa.IntegrationTests.Api;

/// <summary>
/// Regression tests for the findings of the Phase 2 security review (project/progress.md).
/// Each test failed (or could not be written) before its fix.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class SecurityReviewRegressionTests
{
    private readonly SqlServerFixture _db;

    public SecurityReviewRegressionTests(SqlServerFixture db)
    {
        _db = db;
    }

    private static long RoleId(ApiResult roles, string name) =>
        roles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == name).GetProperty("id").GetInt64();

    private static async Task<long> MembershipIdAsync(ApiTestHost api, string adminToken, string email) =>
        (await api.PostAsync("/api/staff/search", adminToken, new { pageNumber = 1, pageSize = 100 }))
            .Data.GetProperty("items").EnumerateArray()
            .Single(m => m.GetProperty("email").GetString() == email).GetProperty("id").GetInt64();

    private static async Task<long> AddStaffAsync(ApiTestHost api, string token, string email, long roleId)
    {
        var result = await api.PostAsync("/api/staff", token, new
        {
            fullName = email,
            email,
            password = ApiTestHost.Password,
            roleIds = new[] { roleId },
            allBranches = true,
        });
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetInt64();
    }

    private async Task<(ApiTestHost Api, string Admin, ApiResult Roles)> OneTenantAsync(int? maxEmployees = null)
    {
        var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC", maxEmployees);
        await api.CreateTenantAsync(platform, planId, "al-noor");
        var admin = await api.AdminTokenAsync("al-noor");
        return (api, admin, await api.GetAsync("/api/roles", admin));
    }

    [Fact]
    public async Task A_staff_manager_cannot_suspend_or_demote_someone_more_powerful()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;

        var staffAdminRole = (await api.PostAsync("/api/roles", admin,
            new { name = "StaffAdmin", permissions = new[] { "Staff.Read", "Staff.Manage", "Roles.Read" } })).Data.GetInt64();
        await AddStaffAsync(api, admin, "staffadmin@al-noor.test", staffAdminRole);
        var staffAdmin = await api.TokenAsync("staffadmin@al-noor.test", ApiTestHost.Password, "al-noor");
        var tenantAdminMembership = await MembershipIdAsync(api, admin, ApiTestHost.AdminEmail("al-noor"));

        var suspend = await api.PutAsync($"/api/staff/{tenantAdminMembership}/suspend", staffAdmin);
        var demote = await api.PutAsync("/api/staff", staffAdmin, new { id = tenantAdminMembership, roleIds = new[] { staffAdminRole }, allBranches = true });

        Assert.Equal(HttpStatusCode.Forbidden, suspend.Status);
        Assert.Equal(HttpStatusCode.Forbidden, demote.Status);
        Assert.Equal(HttpStatusCode.OK, (await api.GetAsync("/api/roles", admin)).Status);   // admin still fully working
    }

    [Fact]
    public async Task The_last_active_TenantAdmin_cannot_lose_the_role_but_can_once_another_admin_exists()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;
        var tenantAdminRole = RoleId(roles, Permissions.TenantAdminRole);
        var doctorRole = RoleId(roles, "Doctor");
        var ownMembership = await MembershipIdAsync(api, admin, ApiTestHost.AdminEmail("al-noor"));

        var alone = await api.PutAsync("/api/staff", admin, new { id = ownMembership, roleIds = new[] { doctorRole }, allBranches = true });
        Assert.Equal(HttpStatusCode.BadRequest, alone.Status);

        await AddStaffAsync(api, admin, "second.admin@al-noor.test", tenantAdminRole);

        var withSecondAdmin = await api.PutAsync("/api/staff", admin, new { id = ownMembership, roleIds = new[] { doctorRole }, allBranches = true });
        Assert.Equal(HttpStatusCode.OK, withSecondAdmin.Status);
    }

    [Fact]
    public async Task A_role_manager_cannot_strip_or_delete_permissions_they_do_not_hold()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;

        var roleAdminRole = (await api.PostAsync("/api/roles", admin, new { name = "RoleAdmin", permissions = new[] { "Roles.Read", "Roles.Manage" } })).Data.GetInt64();
        await AddStaffAsync(api, admin, "roleadmin@al-noor.test", roleAdminRole);
        var roleAdmin = await api.TokenAsync("roleadmin@al-noor.test", ApiTestHost.Password, "al-noor");
        var moneyRole = (await api.PostAsync("/api/roles", admin, new { name = "Money", permissions = new[] { "Billing.Refund" } })).Data.GetInt64();
        var cashierRole = RoleId(roles, "Cashier");

        var strip = await api.PutAsync("/api/roles", roleAdmin, new { id = cashierRole, name = "Cashier", permissions = Array.Empty<string>() });
        var delete = await api.DeleteAsync($"/api/roles/{moneyRole}", roleAdmin);

        Assert.Equal(HttpStatusCode.Forbidden, strip.Status);
        Assert.Equal(HttpStatusCode.Forbidden, delete.Status);
    }

    [Fact]
    public async Task Reactivating_a_member_respects_the_plan_employee_limit()
    {
        var (api, admin, roles) = await OneTenantAsync(maxEmployees: 2);
        await using var _ = api;
        var nurseRole = RoleId(roles, "Nurse");

        var first = await AddStaffAsync(api, admin, "nurse1@al-noor.test", nurseRole);
        Assert.Equal(HttpStatusCode.OK, (await api.PutAsync($"/api/staff/{first}/suspend", admin)).Status);
        await AddStaffAsync(api, admin, "nurse2@al-noor.test", nurseRole);   // admin + nurse2 = 2 active

        var reactivate = await api.PutAsync($"/api/staff/{first}/activate", admin);

        Assert.Equal(HttpStatusCode.BadRequest, reactivate.Status);
    }

    [Fact]
    public async Task Two_concurrent_refreshes_with_the_same_token_cannot_both_succeed()
    {
        var (api, _, _) = await OneTenantAsync();
        await using var _ = api;
        var login = await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "al-noor");
        var refreshToken = login.Data.GetProperty("refreshToken").GetString()!;

        var results = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => api.PostAsync("/api/auth/refresh", null, new { refreshToken })));

        Assert.Single(results, r => r.Status == HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_slug_freed_by_a_removed_tenant_can_be_used_again()
    {
        var api = await ApiTestHost.StartAsync(_db);
        await using var _ = api;
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        var tenantId = await api.CreateTenantAsync(platform, planId, "al-noor");

        // Same as the compensation of a failed onboarding: the tenant is soft-deleted.
        await using (var tenancy = _db.CreateTenancyContext())
        {
            var tenant = await tenancy.Tenants.SingleAsync(t => t.Id == tenantId, TestContext.Current.CancellationToken);
            tenancy.Tenants.Remove(tenant);
            await tenancy.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var again = await api.PostAsync("/api/platform/tenants", platform, new
        {
            name = "Al Noor", slug = "al-noor", planId, adminFullName = "Admin", adminEmail = "new.admin@al-noor.test", adminPassword = ApiTestHost.Password,
        });

        Assert.Equal(HttpStatusCode.OK, again.Status);
        Assert.NotEqual(tenantId, again.Data.GetInt64());
    }

    [Fact]
    public async Task Authentication_and_authorization_failures_use_the_Response_shape()
    {
        var (api, admin, _) = await OneTenantAsync();
        await using var _ = api;

        var unauthenticated = await api.GetAsync("/api/roles", null);
        var forbidden = await api.GetAsync("/api/platform/plans", admin);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.Status);
        Assert.Equal("Authentication is required.", unauthenticated.Message);
        Assert.False(unauthenticated.Body.GetProperty("isSuccess").GetBoolean());

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.Status);
        Assert.Equal("You don't have permission to do this.", forbidden.Message);
    }
}
