using System.Net;
using Shifa.IntegrationTests.Infrastructure;

namespace Shifa.IntegrationTests.Api;

/// <summary>Phase 4: patient and guardian sign-in with a phone OTP, and what the portal token can and cannot do.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class PatientPortalTests
{
    private const string Parent = "+201012345678";
    private const string Stranger = "+201098765432";

    private readonly SqlServerFixture _db;

    public PatientPortalTests(SqlServerFixture db)
    {
        _db = db;
    }

    private sealed record Clinic(ApiTestHost Api, string Admin, long Child1, long Child2, long Unlinked);

    /// <summary>A clinic with a parent (phone <see cref="Parent"/>) linked to two children, and a third patient nobody is linked to.</summary>
    private async Task<Clinic> ClinicAsync(string slug = "al-noor", ApiTestHost? existing = null)
    {
        var api = existing ?? await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, $"PLAN-{slug}");
        await api.CreateTenantAsync(platform, planId, slug);
        var admin = await api.AdminTokenAsync(slug);

        var childBirth = DateTime.UtcNow.AddYears(-6).ToString("yyyy-MM-dd");
        async Task<long> Patient(string name) => (await api.PostAsync("/api/patients", admin, new
        {
            fullName = name,
            dateOfBirth = childBirth,
            gender = "Male",
            guardians = new[] { new { fullName = "Parent Synthetic", relationship = "Mother" } },
        })).Data.GetInt64();

        var child1 = await Patient("Child One");
        var child2 = await Patient("Child Two");
        var unlinked = await Patient("Child Three");
        foreach (var child in new[] { child1, child2 })
            Assert.Equal(HttpStatusCode.OK, (await api.PostAsync("/api/patients/portal-access", admin, new { patientId = child, phoneNumber = "010 1234 5678", relationship = "Parent" })).Status);

        return new Clinic(api, admin, child1, child2, unlinked);
    }

    private static async Task<(string Access, string Refresh)> SignInAsync(ApiTestHost api, string slug, string phone)
    {
        Assert.Equal(HttpStatusCode.OK, (await api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = slug, phoneNumber = phone })).Status);
        var code = api.LastOtp(phone) ?? throw new InvalidOperationException("No SMS was sent.");
        var verified = await api.PostAsync("/api/portal/auth/otp/verify", null, new { tenantSlug = slug, phoneNumber = phone, code });
        Assert.Equal(HttpStatusCode.OK, verified.Status);
        return (verified.Data.GetProperty("accessToken").GetString()!, verified.Data.GetProperty("refreshToken").GetString()!);
    }

    [Fact]
    public async Task A_parent_signs_in_with_an_OTP_and_sees_only_the_linked_children()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;

        var (token, _) = await SignInAsync(c.Api, "al-noor", Parent);

        var mine = await c.Api.GetAsync("/api/portal/patients", token);
        Assert.Equal(HttpStatusCode.OK, mine.Status);
        Assert.Equal(["Child One", "Child Two"], mine.Data.EnumerateArray().Select(p => p.GetProperty("fullName").GetString()).ToArray());
        Assert.All(mine.Data.EnumerateArray(), p => Assert.Equal("Parent", p.GetProperty("relationship").GetString()));
        Assert.False(mine.Data[0].TryGetProperty("identifiers", out _));   // portal view has no internal data

        Assert.Equal(HttpStatusCode.OK, (await c.Api.GetAsync($"/api/portal/patients/{c.Child1}", token)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.GetAsync($"/api/portal/patients/{c.Unlinked}", token)).Status);
    }

    [Fact]
    public async Task OTP_request_never_reveals_which_numbers_the_clinic_knows_and_sends_nothing_for_unknown_ones()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;

        var known = await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = "al-noor", phoneNumber = Parent });
        var unknown = await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = "al-noor", phoneNumber = Stranger });
        var unknownClinic = await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = "no-such-clinic", phoneNumber = Parent });

        Assert.Equal(HttpStatusCode.OK, known.Status);
        Assert.Equal(known.Message, unknown.Message);
        Assert.Equal(known.Message, unknownClinic.Message);
        Assert.Equal(1, c.Api.SmsCount(Parent));
        Assert.Equal(0, c.Api.SmsCount(Stranger));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = "al-noor", phoneNumber = "abc" })).Status);
    }

    [Fact]
    public async Task Wrong_codes_all_look_the_same_the_code_dies_after_five_tries_and_works_only_once()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = "al-noor", phoneNumber = Parent });
        var code = c.Api.LastOtp(Parent)!;
        var wrong = code == "000000" ? "111111" : "000000";

        Task<ApiResult> Verify(string tenant, string phone, string value) =>
            c.Api.PostAsync("/api/portal/auth/otp/verify", null, new { tenantSlug = tenant, phoneNumber = phone, code = value });

        var attempts = new[]
        {
            await Verify("al-noor", Parent, wrong),
            await Verify("al-noor", Stranger, code),      // right code, wrong phone
            await Verify("no-such-clinic", Parent, code), // right code, wrong clinic
        };
        Assert.All(attempts, a => Assert.Equal(HttpStatusCode.Unauthorized, a.Status));
        Assert.Single(attempts.Select(a => a.Message).Distinct());

        for (var i = 0; i < 4; i++)
            await Verify("al-noor", Parent, wrong);   // 5 wrong tries in total
        Assert.Equal(HttpStatusCode.Unauthorized, (await Verify("al-noor", Parent, code)).Status);   // now even the right code is dead

        // a fresh code works once
        await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = "al-noor", phoneNumber = Parent });
        var fresh = c.Api.LastOtp(Parent)!;
        Assert.Equal(HttpStatusCode.OK, (await Verify("al-noor", Parent, fresh)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Verify("al-noor", Parent, fresh)).Status);
    }

    [Fact]
    public async Task At_most_three_codes_are_sent_to_a_number_in_ten_minutes()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;

        for (var i = 0; i < 6; i++)
            Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = "al-noor", phoneNumber = Parent })).Status);

        Assert.Equal(3, c.Api.SmsCount(Parent));
    }

    [Fact]
    public async Task A_portal_token_cannot_use_staff_or_platform_endpoints()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var (token, _) = await SignInAsync(c.Api, "al-noor", Parent);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.PostAsync("/api/patients/search", token, new { })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.GetAsync($"/api/patients/{c.Child1}", token)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.GetAsync("/api/roles", token)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.PostAsync("/api/staff/search", token, new { })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.GetAsync("/api/platform/plans", token)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.PostAsync("/api/auth/switch-tenant", token, new { tenantSlug = "al-noor" })).Status);

        // and a staff token can't use the portal
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.GetAsync("/api/portal/patients", c.Admin)).Status);
    }

    [Fact]
    public async Task A_portal_token_is_bound_to_its_clinic_and_a_phone_is_linked_per_clinic()
    {
        var c = await ClinicAsync("al-noor");
        await using var host = c.Api;
        var other = await ClinicAsync("al-shifa", c.Api);
        var (alNoorToken, _) = await SignInAsync(c.Api, "al-noor", Parent);

        // the same parent is also known to Al Shifa, with different children
        var (alShifaToken, _) = await SignInAsync(c.Api, "al-shifa", Parent);
        var alNoorView = await c.Api.GetAsync("/api/portal/patients", alNoorToken);
        var alShifaView = await c.Api.GetAsync("/api/portal/patients", alShifaToken);
        var alNoorIds = alNoorView.Data.EnumerateArray().Select(p => p.GetProperty("id").GetInt64()).ToHashSet();
        var alShifaIds = alShifaView.Data.EnumerateArray().Select(p => p.GetProperty("id").GetInt64()).ToHashSet();

        Assert.Empty(alNoorIds.Intersect(alShifaIds));
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.GetAsync($"/api/portal/patients/{other.Child1}", alNoorToken)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.GetAsync($"/api/portal/patients/{c.Child1}", alShifaToken)).Status);
    }

    [Fact]
    public async Task Removing_the_access_link_or_suspending_the_clinic_takes_effect_and_refresh_keeps_the_session_portal_only()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var (token, refresh) = await SignInAsync(c.Api, "al-noor", Parent);

        // refresh issues another portal token, not a staff one
        var refreshed = await c.Api.PostAsync("/api/auth/refresh", null, new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.OK, refreshed.Status);
        var token2 = refreshed.Data.GetProperty("accessToken").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await c.Api.GetAsync("/api/portal/patients", token2)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.GetAsync("/api/roles", token2)).Status);

        // the clinic removes the link of child 1
        var view = await c.Api.GetAsync($"/api/patients/{c.Child1}", c.Admin);
        var accessId = view.Data.GetProperty("portalAccess")[0].GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.OK, (await c.Api.DeleteAsync($"/api/patients/portal-access/{accessId}", c.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.GetAsync($"/api/portal/patients/{c.Child1}", token)).Status);
        Assert.Equal(1, (await c.Api.GetAsync("/api/portal/patients", token)).Data.GetArrayLength());

        // suspending the clinic locks the portal out too, including refresh
        var platform = await c.Api.PlatformTokenAsync();
        var tenantId = (await c.Api.PostAsync("/api/platform/tenants/search", platform, new { pageNumber = 1, pageSize = 10 })).Data.GetProperty("items")[0].GetProperty("id").GetInt64();
        await c.Api.PutAsync($"/api/platform/tenants/{tenantId}/suspend", platform);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.Api.GetAsync("/api/portal/patients", token2)).Status);
        var newRefresh = refreshed.Data.GetProperty("refreshToken").GetString()!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.Api.PostAsync("/api/auth/refresh", null, new { refreshToken = newRefresh })).Status);
    }

    [Fact]
    public async Task A_portal_user_has_no_password_login()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        await SignInAsync(c.Api, "al-noor", Parent);   // creates the phone-only user

        foreach (var password in new[] { "Clinic-Pass-12345", "password", Parent })
        {
            var login = await c.Api.LoginAsync(Parent, password, "al-noor");
            Assert.Equal(HttpStatusCode.Unauthorized, login.Status);
        }
    }

    [Fact]
    public async Task Linking_portal_access_validates_the_phone_and_does_not_create_duplicates()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;

        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PostAsync("/api/patients/portal-access", c.Admin, new { patientId = c.Unlinked, phoneNumber = "nope", relationship = "Parent" })).Status);

        var first = await c.Api.PostAsync("/api/patients/portal-access", c.Admin, new { patientId = c.Unlinked, phoneNumber = "01098765432", relationship = "Guardian" });
        var again = await c.Api.PostAsync("/api/patients/portal-access", c.Admin, new { patientId = c.Unlinked, phoneNumber = "+20 109 876 5432", relationship = "Parent", canBook = false });
        Assert.Equal(first.Data.GetInt64(), again.Data.GetInt64());

        var view = await c.Api.GetAsync($"/api/patients/{c.Unlinked}", c.Admin);
        Assert.Equal(1, view.Data.GetProperty("portalAccess").GetArrayLength());
        Assert.Equal("Parent", view.Data.GetProperty("portalAccess")[0].GetProperty("relationship").GetString());
        Assert.False(view.Data.GetProperty("portalAccess")[0].GetProperty("canBook").GetBoolean());
    }
}
