using System.Net;
using Microsoft.EntityFrameworkCore;
using Core.Common;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>Roles, permissions, privilege escalation, plan limits and the Platform Admin boundary.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class AuthorizationAndPlatformTests
{
    private readonly SqlServerFixture _db;

    public AuthorizationAndPlatformTests(SqlServerFixture db)
    {
        _db = db;
    }

    private static long RoleId(ApiResult roles, string name) =>
        roles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == name).GetProperty("id").GetInt64();

    private async Task<(ApiTestHost Api, string Admin, ApiResult Roles)> OneTenantAsync(int? maxEmployees = null)
    {
        var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC", maxEmployees);
        await api.CreateTenantAsync(platform, planId, "al-noor");
        var admin = await api.AdminTokenAsync("al-noor");
        return (api, admin, await api.GetAsync("/api/roles", admin));
    }

    private static async Task<string> AddStaffAndSignInAsync(ApiTestHost api, string adminToken, string email, long roleId)
    {
        var added = await api.PostAsync("/api/staff", adminToken, new
        {
            fullName = email,
            email,
            password = ApiTestHost.Password,
            roleIds = new[] { roleId },
            allBranches = true,
        });
        Assert.Equal(HttpStatusCode.OK, added.Status);
        return await api.TokenAsync(email, ApiTestHost.Password, "al-noor");
    }

    [Fact]
    public async Task Onboarding_creates_the_default_roles_and_a_TenantAdmin_with_every_permission()
    {
        var (api, _, roles) = await OneTenantAsync();
        await using var _ = api;

        var names = roles.Data.EnumerateArray().Select(r => r.GetProperty("name").GetString()!).Order().ToList();
        Assert.Equal(Permissions.DefaultRoles.Keys.Order().ToList(), names);

        var admin = roles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == Permissions.TenantAdminRole);
        Assert.Equal(Permissions.All.Count, admin.GetProperty("permissions").GetArrayLength());
    }

    [Fact]
    public async Task A_member_without_the_permission_gets_403()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;
        var receptionist = await AddStaffAndSignInAsync(api, admin, "reception@al-noor.test", RoleId(roles, "Receptionist"));

        Assert.Equal(HttpStatusCode.Forbidden, (await api.GetAsync("/api/roles", receptionist)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.PostAsync("/api/staff/search", receptionist, new { })).Status);
    }

    [Fact]
    public async Task Staff_manager_cannot_grant_a_role_with_more_power_than_their_own()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;

        var staffAdminRole = (await api.PostAsync("/api/roles", admin, new { name = "StaffAdmin", permissions = new[] { "Staff.Read", "Staff.Manage", "Roles.Read" } })).Data.GetInt64();
        var staffAdmin = await AddStaffAndSignInAsync(api, admin, "staffadmin@al-noor.test", staffAdminRole);

        var escalation = await api.PostAsync("/api/staff", staffAdmin, new
        {
            fullName = "Sneaky",
            email = "sneaky@al-noor.test",
            password = ApiTestHost.Password,
            roleIds = new[] { RoleId(roles, Permissions.TenantAdminRole) },
            allBranches = true,
        });
        var allowed = await api.PostAsync("/api/staff", staffAdmin, new
        {
            fullName = "Fine",
            email = "fine@al-noor.test",
            password = ApiTestHost.Password,
            roleIds = new[] { staffAdminRole },
            allBranches = true,
        });

        Assert.Equal(HttpStatusCode.Forbidden, escalation.Status);
        Assert.Equal(HttpStatusCode.OK, allowed.Status);
    }

    [Fact]
    public async Task Role_manager_cannot_create_a_role_with_permissions_they_lack()
    {
        var (api, admin, _) = await OneTenantAsync();
        await using var _ = api;

        var roleAdminRole = (await api.PostAsync("/api/roles", admin, new { name = "RoleAdmin", permissions = new[] { "Roles.Read", "Roles.Manage" } })).Data.GetInt64();
        var roleAdmin = await AddStaffAndSignInAsync(api, admin, "roleadmin@al-noor.test", roleAdminRole);

        var escalation = await api.PostAsync("/api/roles", roleAdmin, new { name = "Money", permissions = new[] { "Billing.Refund" } });
        var allowed = await api.PostAsync("/api/roles", roleAdmin, new { name = "ReadOnly", permissions = new[] { "Roles.Read" } });

        Assert.Equal(HttpStatusCode.Forbidden, escalation.Status);
        Assert.Equal(HttpStatusCode.OK, allowed.Status);
    }

    [Fact]
    public async Task System_roles_are_protected_and_unknown_permissions_are_rejected()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;
        var tenantAdminRole = RoleId(roles, Permissions.TenantAdminRole);
        var doctorRole = RoleId(roles, "Doctor");

        Assert.Equal(HttpStatusCode.BadRequest, (await api.PutAsync("/api/roles", admin, new { id = tenantAdminRole, name = Permissions.TenantAdminRole, permissions = new[] { "Roles.Read" } })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteAsync($"/api/roles/{doctorRole}", admin)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.PostAsync("/api/roles", admin, new { name = "Bad", permissions = new[] { "Everything.Allowed" } })).Status);
    }

    [Fact]
    public async Task A_suspended_staff_member_loses_access_immediately()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;
        var token = await AddStaffAndSignInAsync(api, admin, "nurse@al-noor.test", RoleId(roles, "Nurse"));
        var membershipId = (await api.PostAsync("/api/staff/search", admin, new { pageNumber = 1, pageSize = 50 }))
            .Data.GetProperty("items").EnumerateArray().Single(m => m.GetProperty("email").GetString() == "nurse@al-noor.test").GetProperty("id").GetInt64();

        Assert.Equal(HttpStatusCode.Forbidden, (await api.GetAsync("/api/roles", token)).Status);   // authenticated, just not allowed

        Assert.Equal(HttpStatusCode.OK, (await api.PutAsync($"/api/staff/{membershipId}/suspend", admin)).Status);

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/roles", token)).Status);
    }

    [Fact]
    public async Task Admin_cannot_suspend_their_own_membership()
    {
        var (api, admin, _) = await OneTenantAsync();
        await using var _ = api;
        var own = (await api.PostAsync("/api/staff/search", admin, new { pageNumber = 1, pageSize = 10 })).Data.GetProperty("items")[0].GetProperty("id").GetInt64();

        Assert.Equal(HttpStatusCode.BadRequest, (await api.PutAsync($"/api/staff/{own}/suspend", admin)).Status);
    }

    [Fact]
    public async Task Plan_employee_limit_is_enforced()
    {
        var (api, admin, roles) = await OneTenantAsync(maxEmployees: 1);   // the TenantAdmin already counts as 1
        await using var _ = api;

        var result = await api.PostAsync("/api/staff", admin, new
        {
            fullName = "One too many",
            email = "extra@al-noor.test",
            password = ApiTestHost.Password,
            roleIds = new[] { RoleId(roles, "Nurse") },
            allBranches = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
    }

    [Fact]
    public async Task Unknown_branch_ids_are_rejected()
    {
        var (api, admin, roles) = await OneTenantAsync();
        await using var _ = api;

        var result = await api.PostAsync("/api/staff", admin, new
        {
            fullName = "Branch staff",
            email = "branch@al-noor.test",
            password = ApiTestHost.Password,
            roleIds = new[] { RoleId(roles, "Nurse") },
            allBranches = false,
            branchIds = new[] { 987654321L },
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
    }

    [Fact]
    public async Task Platform_endpoints_reject_anonymous_callers_and_tenant_admins()
    {
        var (api, admin, _) = await OneTenantAsync();
        await using var _ = api;

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.PostAsync("/api/platform/tenants/search", null, new { })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.PostAsync("/api/platform/tenants/search", admin, new { })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.GetAsync("/api/platform/plans", admin)).Status);
    }

    [Fact]
    public async Task Tenant_onboarding_validates_input_and_rejects_duplicates()
    {
        var api = await ApiTestHost.StartAsync(_db);
        await using var _ = api;
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        await api.CreateTenantAsync(platform, planId, "al-noor");

        object Tenant(string slug, string password = ApiTestHost.Password) => new
        {
            name = "X", slug, planId, adminFullName = "Admin", adminEmail = $"admin@{slug}.example.test", adminPassword = password,
        };

        Assert.Equal(HttpStatusCode.BadRequest, (await api.PostAsync("/api/platform/tenants", platform, Tenant("al-noor"))).Status);       // duplicate slug
        Assert.Equal(HttpStatusCode.BadRequest, (await api.PostAsync("/api/platform/tenants", platform, Tenant("Bad Slug!"))).Status);      // invalid slug
        Assert.Equal(HttpStatusCode.BadRequest, (await api.PostAsync("/api/platform/tenants", platform, Tenant("weak-one", "short"))).Status); // weak password
        Assert.Equal(HttpStatusCode.OK, (await api.PostAsync("/api/platform/tenants", platform, Tenant("fine-one"))).Status);
    }

    [Fact]
    public async Task Platform_actions_are_audited_with_the_acting_user_and_target_tenant()
    {
        var api = await ApiTestHost.StartAsync(_db);
        await using var _ = api;
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        var tenantId = await api.CreateTenantAsync(platform, planId, "al-noor");
        await api.PutAsync($"/api/platform/tenants/{tenantId}/suspend", platform);

        await using var identity = _db.CreateIdentityContext();
        var audit = await identity.AuditLogs.Where(a => a.TargetTenantId == tenantId).Select(a => new { a.Action, a.UserId }).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains(audit, a => a.Action == "Platform.TenantCreated" && a.UserId != null);
        Assert.Contains(audit, a => a.Action == "Platform.TenantSuspended" && a.UserId != null);
    }

    [Fact]
    public async Task Tenant_list_shows_plan_and_subscription_state()
    {
        var api = await ApiTestHost.StartAsync(_db);
        await using var _ = api;
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        await api.CreateTenantAsync(platform, planId, "al-noor");

        var search = await api.PostAsync("/api/platform/tenants/search", platform, new { pageNumber = 1, pageSize = 10 });
        var tenant = search.Data.GetProperty("items")[0];

        Assert.Equal("al-noor", tenant.GetProperty("slug").GetString());
        Assert.Equal("BASIC", tenant.GetProperty("planCode").GetString());
        Assert.Equal("Trial", tenant.GetProperty("subscriptionStatus").GetString());
        Assert.Equal("Trial", tenant.GetProperty("status").GetString());
    }
}
