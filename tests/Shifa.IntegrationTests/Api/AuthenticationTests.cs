using System.Net;
using Shifa.IntegrationTests.Infrastructure;

namespace Shifa.IntegrationTests.Api;

/// <summary>Sign-in, tokens and sessions (ADR-007).</summary>
[Collection(SqlServerCollection.Name)]
public sealed class AuthenticationTests
{
    private readonly SqlServerFixture _db;

    public AuthenticationTests(SqlServerFixture db)
    {
        _db = db;
    }

    [Fact]
    public async Task Valid_credentials_return_tokens_for_the_requested_tenant()
    {
        await using var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        var tenantId = await api.CreateTenantAsync(platform, planId, "al-noor");

        var result = await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "al-noor");

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.False(string.IsNullOrEmpty(result.Data.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(result.Data.GetProperty("refreshToken").GetString()));
        Assert.Equal(tenantId, result.Data.GetProperty("tenantId").GetInt64());
        Assert.False(result.Data.GetProperty("isPlatformAdmin").GetBoolean());
    }

    [Fact]
    public async Task Every_failed_sign_in_gets_the_same_401_so_nothing_is_revealed()
    {
        await using var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        await api.CreateTenantAsync(platform, planId, "al-noor");
        await api.CreateTenantAsync(platform, planId, "al-shifa");

        var attempts = new[]
        {
            await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), "Wrong-Password-1", "al-noor"),   // wrong password
            await api.LoginAsync("nobody@shifa.test", ApiTestHost.Password, "al-noor"),                // unknown user
            await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "al-shifa"), // real user, other tenant
            await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "no-such"),  // unknown tenant
            await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password),             // staff without a tenant
        };

        Assert.All(attempts, a => Assert.Equal(HttpStatusCode.Unauthorized, a.Status));
        Assert.Single(attempts.Select(a => a.Message).Distinct());
    }

    [Fact]
    public async Task Account_is_locked_after_five_wrong_passwords()
    {
        await using var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        await api.CreateTenantAsync(platform, planId, "al-noor");

        for (var i = 0; i < 5; i++)
            await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), "Wrong-Password-1", "al-noor");

        var correct = await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "al-noor");

        Assert.Equal(HttpStatusCode.Unauthorized, correct.Status);
    }

    [Fact]
    public async Task Refresh_token_rotates_and_reusing_an_old_one_revokes_the_whole_family()
    {
        await using var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        await api.CreateTenantAsync(platform, planId, "al-noor");

        var login = await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "al-noor");
        var first = login.Data.GetProperty("refreshToken").GetString()!;

        var rotated = await api.PostAsync("/api/auth/refresh", null, new { refreshToken = first });
        Assert.Equal(HttpStatusCode.OK, rotated.Status);
        var second = rotated.Data.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(first, second);

        var replay = await api.PostAsync("/api/auth/refresh", null, new { refreshToken = first });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.Status);

        // The replay is treated as theft: the newer token of the same family is dead too.
        var afterTheft = await api.PostAsync("/api/auth/refresh", null, new { refreshToken = second });
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.Status);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        await using var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        await api.CreateTenantAsync(platform, planId, "al-noor");

        var login = await api.LoginAsync(ApiTestHost.AdminEmail("al-noor"), ApiTestHost.Password, "al-noor");
        var refresh = login.Data.GetProperty("refreshToken").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await api.PostAsync("/api/auth/logout", null, new { refreshToken = refresh })).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.PostAsync("/api/auth/refresh", null, new { refreshToken = refresh })).Status);
    }

    [Fact]
    public async Task One_person_can_belong_to_two_tenants_and_switch_between_them()
    {
        await using var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, "BASIC");
        await api.CreateTenantAsync(platform, planId, "al-noor");
        var alShifaId = await api.CreateTenantAsync(platform, planId, "al-shifa");

        var alNoorAdminToken = await api.AdminTokenAsync("al-noor");
        var switchBefore = await api.PostAsync("/api/auth/switch-tenant", alNoorAdminToken, new { tenantSlug = "al-shifa" });
        Assert.Equal(HttpStatusCode.Unauthorized, switchBefore.Status);   // no membership in Al Shifa yet

        // Al Shifa's admin adds Al Noor's admin (same account, matched by email) to Al Shifa.
        var alShifaAdminToken = await api.AdminTokenAsync("al-shifa");
        var roles = await api.GetAsync("/api/roles", alShifaAdminToken);
        var receptionist = roles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Receptionist").GetProperty("id").GetInt64();
        var added = await api.PostAsync("/api/staff", alShifaAdminToken, new
        {
            fullName = "Admin of al-noor",
            email = ApiTestHost.AdminEmail("al-noor"),
            roleIds = new[] { receptionist },
            allBranches = true,
        });
        Assert.Equal(HttpStatusCode.OK, added.Status);

        var switched = await api.PostAsync("/api/auth/switch-tenant", alNoorAdminToken, new { tenantSlug = "al-shifa" });
        Assert.Equal(HttpStatusCode.OK, switched.Status);
        Assert.Equal(alShifaId, switched.Data.GetProperty("tenantId").GetInt64());
    }

    [Fact]
    public async Task A_made_up_or_missing_token_is_rejected()
    {
        await using var api = await ApiTestHost.StartAsync(_db);

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/roles", null)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/roles", "not-a-real-token")).Status);
    }

    [Fact]
    public async Task A_platform_token_works_on_platform_endpoints_but_has_no_tenant_permissions()
    {
        await using var api = await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();

        Assert.Equal(HttpStatusCode.OK, (await api.GetAsync("/api/platform/plans", platform)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.GetAsync("/api/roles", platform)).Status);
    }
}
