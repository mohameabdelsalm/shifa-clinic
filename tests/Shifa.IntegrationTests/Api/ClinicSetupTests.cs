using System.Net;
using Microsoft.EntityFrameworkCore;
using Shifa.IntegrationTests.Infrastructure;

namespace Shifa.IntegrationTests.Api;

/// <summary>Phase 3: branches, specialties, doctors (branches, schedules, time off), services and prices.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class ClinicSetupTests
{
    private readonly SqlServerFixture _db;

    public ClinicSetupTests(SqlServerFixture db)
    {
        _db = db;
    }

    private sealed record Org(ApiTestHost Api, string Admin, long TenantId);

    private async Task<Org> OrgAsync(string slug = "al-noor", int? maxBranches = null, int? maxDoctors = null, ApiTestHost? existing = null)
    {
        var api = existing ?? await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, $"PLAN-{slug}", maxBranches: maxBranches, maxDoctors: maxDoctors);
        var tenantId = await api.CreateTenantAsync(platform, planId, slug);
        return new Org(api, await api.AdminTokenAsync(slug), tenantId);
    }

    private static async Task<long> AddBranchAsync(Org o, string code) =>
        (await o.Api.PostAsync("/api/branches", o.Admin, new { name = $"Branch {code}", code })).Data.GetInt64();

    private static async Task<long> AddSpecialtyAsync(Org o, string nameEn = "Cardiology") =>
        (await o.Api.PostAsync("/api/specialties", o.Admin, new { nameEn, nameAr = "قلب" })).Data.GetInt64();

    private static async Task<long> AddDoctorAsync(Org o, long specialtyId, params long[] branchIds)
    {
        var result = await o.Api.PostAsync("/api/doctors", o.Admin, new
        {
            fullNameEn = "Dr. Synthetic",
            fullNameAr = "د. تجريبي",
            title = "Consultant",
            specialtyIds = new[] { specialtyId },
            branchIds,
        });
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetInt64();
    }

    private static async Task<long> AddServiceAsync(Org o, long specialtyId, string nameEn = "Consultation") =>
        (await o.Api.PostAsync("/api/services", o.Admin, new { specialtyId, nameEn, nameAr = "كشف", durationMinutes = 30 })).Data.GetInt64();

    /// <summary>A member limited to <paramref name="branchIds"/> who can manage branches, doctors and services there.</summary>
    private static async Task<string> BranchOpsAsync(Org o, string email, params long[] branchIds)
    {
        var roleId = (await o.Api.PostAsync("/api/roles", o.Admin, new
        {
            name = $"BranchOps-{email}",
            permissions = new[] { "Branches.Read", "Branches.Manage", "Doctors.Read", "Doctors.Manage", "Services.Read", "Services.Manage" },
        })).Data.GetInt64();

        var added = await o.Api.PostAsync("/api/staff", o.Admin, new
        {
            fullName = email,
            email,
            password = ApiTestHost.Password,
            roleIds = new[] { roleId },
            allBranches = false,
            branchIds,
        });
        Assert.Equal(HttpStatusCode.OK, added.Status);
        return await o.Api.TokenAsync(email, ApiTestHost.Password, "al-noor");
    }

    [Fact]
    public async Task Branches_can_be_created_updated_and_deactivated_with_unique_code_and_name()
    {
        var o = await OrgAsync();
        await using var _ = o.Api;

        var id = await AddBranchAsync(o, "nasr");
        var view = await o.Api.GetAsync($"/api/branches/{id}", o.Admin);
        Assert.Equal("NASR", view.Data.GetProperty("code").GetString());   // codes are stored upper case
        Assert.Equal("Africa/Cairo", view.Data.GetProperty("timeZoneId").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.PostAsync("/api/branches", o.Admin, new { name = "Other", code = "NASR" })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.PostAsync("/api/branches", o.Admin, new { name = "Branch nasr", code = "X1" })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.PostAsync("/api/branches", o.Admin, new { name = "Bad zone", code = "Z1", timeZoneId = "Mars/Base" })).Status);

        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync("/api/branches", o.Admin, new { id, name = "Nasr City", code = "NASR", timeZoneId = "Africa/Cairo", phone = "0100" })).Status);
        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync($"/api/branches/{id}/status/Inactive", o.Admin)).Status);

        var active = await o.Api.GetAsync("/api/branches/all", o.Admin);
        Assert.Equal(0, active.Data.GetArrayLength());
    }

    [Fact]
    public async Task Tenant_B_cannot_see_or_change_tenant_A_clinic_data()
    {
        var a = await OrgAsync("al-noor");
        await using var _ = a.Api;
        var b = await OrgAsync("al-shifa", existing: a.Api);

        var branchId = await AddBranchAsync(a, "MAIN");
        var specialtyId = await AddSpecialtyAsync(a);
        var doctorId = await AddDoctorAsync(a, specialtyId, branchId);
        var serviceId = await AddServiceAsync(a, specialtyId);

        // read
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.GetAsync($"/api/branches/{branchId}", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.GetAsync($"/api/doctors/{doctorId}", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.GetAsync($"/api/services/{serviceId}", b.Admin)).Status);

        // update / status / delete-like actions
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync("/api/branches", b.Admin, new { id = branchId, name = "X", code = "X" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync($"/api/branches/{branchId}/status/Inactive", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync($"/api/doctors/{doctorId}/status/Inactive", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync($"/api/specialties/{specialtyId}/status/Inactive", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync($"/api/services/{serviceId}/status/Inactive", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync("/api/services/price", b.Admin, new { serviceId, branchId, price = 1 })).Status);

        // using A's ids inside B's own data
        var bSpecialty = await AddSpecialtyAsync(b, "Dermatology");
        var bBranch = await AddBranchAsync(b, "MAIN");
        var bDoctor = await AddDoctorAsync(b, bSpecialty, bBranch);
        var foreignSpecialty = await b.Api.PostAsync("/api/doctors", b.Admin, new { fullNameEn = "X", fullNameAr = "س", specialtyIds = new[] { specialtyId }, branchIds = new[] { bBranch } });
        var foreignBranch = await b.Api.PostAsync("/api/doctors", b.Admin, new { fullNameEn = "X", fullNameAr = "س", specialtyIds = new[] { bSpecialty }, branchIds = new[] { branchId } });
        var foreignSchedule = await b.Api.PutAsync("/api/doctors/schedule", b.Admin, new { doctorId, branchId = bBranch, items = Array.Empty<object>() });
        var foreignServiceSpecialty = await b.Api.PostAsync("/api/services", b.Admin, new { specialtyId, nameEn = "Cross", nameAr = "س", durationMinutes = 20 });
        Assert.Equal(HttpStatusCode.BadRequest, foreignSpecialty.Status);
        Assert.Equal(HttpStatusCode.BadRequest, foreignBranch.Status);
        Assert.Equal(HttpStatusCode.BadRequest, foreignSchedule.Status);   // A's doctor doesn't work at B's branch
        Assert.Equal(HttpStatusCode.BadRequest, foreignServiceSpecialty.Status);
        Assert.NotEqual(0, bDoctor);

        // lists
        var doctors = await b.Api.PostAsync("/api/doctors/search", b.Admin, new { pageNumber = 1, pageSize = 50 });
        Assert.Equal(1, doctors.Data.GetProperty("items").GetArrayLength());
        Assert.Equal(1, (await b.Api.GetAsync("/api/specialties", b.Admin)).Data.GetArrayLength());

        // A's data is untouched
        var stillThere = await a.Api.GetAsync($"/api/doctors/{doctorId}", a.Admin);
        Assert.Equal("Active", stillThere.Data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_member_limited_to_one_branch_sees_and_changes_only_that_branch()
    {
        var o = await OrgAsync();
        await using var _ = o.Api;
        var nasr = await AddBranchAsync(o, "NASR");
        var maadi = await AddBranchAsync(o, "MAADI");
        var specialtyId = await AddSpecialtyAsync(o);
        var serviceId = await AddServiceAsync(o, specialtyId);
        var nasrOps = await BranchOpsAsync(o, "nasr.ops@al-noor.test", nasr);

        // branches
        var visible = await o.Api.PostAsync("/api/branches/search", nasrOps, new { pageNumber = 1, pageSize = 50 });
        Assert.Equal(["NASR"], visible.Data.GetProperty("items").EnumerateArray().Select(b => b.GetProperty("code").GetString()).ToArray());
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.GetAsync($"/api/branches/{maadi}", nasrOps)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.PutAsync("/api/branches", nasrOps, new { id = maadi, name = "X", code = "X" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.PutAsync($"/api/branches/{maadi}/status/Inactive", nasrOps)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await o.Api.PostAsync("/api/branches", nasrOps, new { name = "New", code = "NEW" })).Status);   // org-level action
        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync("/api/branches", nasrOps, new { id = nasr, name = "Nasr City", code = "NASR" })).Status);

        // prices
        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync("/api/services/price", nasrOps, new { serviceId, branchId = nasr, price = 300 })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.PutAsync("/api/services/price", nasrOps, new { serviceId, branchId = maadi, price = 300 })).Status);
        await o.Api.PutAsync("/api/services/price", o.Admin, new { serviceId, branchId = maadi, price = 500 });
        var nasrView = await o.Api.GetAsync($"/api/services/{serviceId}", nasrOps);
        var adminView = await o.Api.GetAsync($"/api/services/{serviceId}", o.Admin);
        Assert.Equal(1, nasrView.Data.GetProperty("prices").GetArrayLength());   // only its own branch's price
        Assert.Equal(2, adminView.Data.GetProperty("prices").GetArrayLength());
    }

    [Fact]
    public async Task Doctors_are_visible_per_branch_and_a_scoped_member_cannot_touch_the_other_branches()
    {
        var o = await OrgAsync();
        await using var _ = o.Api;
        var nasr = await AddBranchAsync(o, "NASR");
        var maadi = await AddBranchAsync(o, "MAADI");
        var specialtyId = await AddSpecialtyAsync(o);
        var maadiOnly = await AddDoctorAsync(o, specialtyId, maadi);
        var shared = await AddDoctorAsync(o, specialtyId, nasr, maadi);
        var nasrOps = await BranchOpsAsync(o, "nasr.ops@al-noor.test", nasr);

        // visibility
        var search = await o.Api.PostAsync("/api/doctors/search", nasrOps, new { pageNumber = 1, pageSize = 50 });
        var ids = search.Data.GetProperty("items").EnumerateArray().Select(d => d.GetProperty("id").GetInt64()).ToList();
        Assert.Equal([shared], ids);
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.GetAsync($"/api/doctors/{maadiOnly}", nasrOps)).Status);

        // the shared doctor's DTO hides the other branch
        var view = await o.Api.GetAsync($"/api/doctors/{shared}", nasrOps);
        Assert.Equal([nasr], view.Data.GetProperty("branchIds").EnumerateArray().Select(b => b.GetInt64()).ToArray());

        // actions that affect the doctor everywhere are refused, doctor-at-other-branch changes are invisible
        Assert.Equal(HttpStatusCode.Forbidden, (await o.Api.PutAsync($"/api/doctors/{shared}/status/Inactive", nasrOps)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await o.Api.PostAsync("/api/doctors/time-off", nasrOps,
            new { doctorId = shared, fromAt = "2030-01-01T00:00:00Z", toAt = "2030-01-05T00:00:00Z" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.PutAsync($"/api/doctors/{maadiOnly}/status/Inactive", nasrOps)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.PutAsync("/api/doctors/schedule", nasrOps, new { doctorId = shared, branchId = maadi, items = Array.Empty<object>() })).Status);

        // an update touches only the caller's branches: asking for no branches removes Nasr, never Maadi
        var update = await o.Api.PutAsync("/api/doctors", nasrOps, new
        {
            id = shared, fullNameEn = "Dr. Shared", fullNameAr = "د. مشترك", specialtyIds = new[] { specialtyId }, branchIds = Array.Empty<long>(),
        });
        Assert.Equal(HttpStatusCode.OK, update.Status);
        var adminView = await o.Api.GetAsync($"/api/doctors/{shared}", o.Admin);
        Assert.Equal([maadi], adminView.Data.GetProperty("branchIds").EnumerateArray().Select(b => b.GetInt64()).ToArray());

        // and they can't add a doctor to a branch outside their scope
        var outside = await o.Api.PostAsync("/api/doctors", nasrOps, new { fullNameEn = "X", fullNameAr = "س", specialtyIds = new[] { specialtyId }, branchIds = new[] { maadi } });
        Assert.Equal(HttpStatusCode.BadRequest, outside.Status);
    }

    [Fact]
    public async Task Plan_limits_on_branches_and_doctors_are_enforced()
    {
        var o = await OrgAsync(maxBranches: 1, maxDoctors: 1);
        await using var _ = o.Api;

        var branchId = await AddBranchAsync(o, "ONE");
        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.PostAsync("/api/branches", o.Admin, new { name = "Two", code = "TWO" })).Status);

        var specialtyId = await AddSpecialtyAsync(o);
        await AddDoctorAsync(o, specialtyId, branchId);
        var second = await o.Api.PostAsync("/api/doctors", o.Admin, new { fullNameEn = "X", fullNameAr = "س", specialtyIds = new[] { specialtyId }, branchIds = new[] { branchId } });
        Assert.Equal(HttpStatusCode.BadRequest, second.Status);
    }

    [Fact]
    public async Task Doctor_schedules_are_validated_and_replace_the_previous_one()
    {
        var o = await OrgAsync();
        await using var _ = o.Api;
        var nasr = await AddBranchAsync(o, "NASR");
        var maadi = await AddBranchAsync(o, "MAADI");
        var doctorId = await AddDoctorAsync(o, await AddSpecialtyAsync(o), nasr, maadi);

        object Item(string day, string start, string end, int slot = 30) => new { dayOfWeek = day, startTime = start, endTime = end, slotMinutes = slot };
        Task<ApiResult> Set(long branchId, params object[] items) => o.Api.PutAsync("/api/doctors/schedule", o.Admin, new { doctorId, branchId, items });

        Assert.Equal(HttpStatusCode.OK, (await Set(nasr, Item("Sunday", "09:00:00", "13:00:00"), Item("Monday", "09:00:00", "13:00:00"))).Status);

        Assert.Equal(HttpStatusCode.BadRequest, (await Set(nasr, Item("Sunday", "13:00:00", "09:00:00"))).Status);                          // end before start
        Assert.Equal(HttpStatusCode.BadRequest, (await Set(nasr, Item("Sunday", "09:00:00", "09:50:00", 30))).Status);                      // not whole slots
        Assert.Equal(HttpStatusCode.BadRequest, (await Set(nasr, Item("Sunday", "09:00:00", "12:00:00"), Item("Sunday", "11:00:00", "13:00:00"))).Status); // overlap inside request
        Assert.Equal(HttpStatusCode.BadRequest, (await Set(maadi, Item("Sunday", "12:00:00", "15:00:00"))).Status);                         // overlaps Nasr hours
        Assert.Equal(HttpStatusCode.OK, (await Set(maadi, Item("Sunday", "13:00:00", "17:00:00"))).Status);                                  // touches but doesn't overlap

        // replaces: Nasr keeps only the new window
        Assert.Equal(HttpStatusCode.OK, (await Set(nasr, Item("Tuesday", "10:00:00", "12:00:00", 20))).Status);
        var view = await o.Api.GetAsync($"/api/doctors/{doctorId}", o.Admin);
        var nasrSchedule = view.Data.GetProperty("schedules").EnumerateArray().Where(s => s.GetProperty("branchId").GetInt64() == nasr).ToList();
        Assert.Single(nasrSchedule);
        Assert.Equal("Tuesday", nasrSchedule[0].GetProperty("dayOfWeek").GetString());

        // removing a branch from the doctor drops its schedule
        await o.Api.PutAsync("/api/doctors", o.Admin, new
        {
            id = doctorId, fullNameEn = "Dr. Synthetic", fullNameAr = "د. تجريبي",
            specialtyIds = view.Data.GetProperty("specialtyIds").EnumerateArray().Select(s => s.GetInt64()).ToArray(), branchIds = new[] { maadi },
        });
        var after = await o.Api.GetAsync($"/api/doctors/{doctorId}", o.Admin);
        Assert.All(after.Data.GetProperty("schedules").EnumerateArray(), s => Assert.Equal(maadi, s.GetProperty("branchId").GetInt64()));
        Assert.Equal(HttpStatusCode.BadRequest, (await Set(nasr, Item("Sunday", "09:00:00", "10:00:00"))).Status);   // no longer works at Nasr
    }

    [Fact]
    public async Task Time_off_can_be_added_validated_and_removed()
    {
        var o = await OrgAsync();
        await using var _ = o.Api;
        var doctorId = await AddDoctorAsync(o, await AddSpecialtyAsync(o), await AddBranchAsync(o, "NASR"));

        var invalid = await o.Api.PostAsync("/api/doctors/time-off", o.Admin, new { doctorId, fromAt = "2030-01-05T00:00:00Z", toAt = "2030-01-01T00:00:00Z" });
        var added = await o.Api.PostAsync("/api/doctors/time-off", o.Admin, new { doctorId, fromAt = "2030-01-01T00:00:00Z", toAt = "2030-01-05T00:00:00Z", reason = "Conference" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.Status);
        Assert.Equal(HttpStatusCode.OK, added.Status);

        var view = await o.Api.GetAsync($"/api/doctors/{doctorId}", o.Admin);
        Assert.Equal(1, view.Data.GetProperty("timeOffs").GetArrayLength());

        Assert.Equal(HttpStatusCode.OK, (await o.Api.DeleteAsync($"/api/doctors/time-off/{added.Data.GetInt64()}", o.Admin)).Status);
        Assert.Equal(0, (await o.Api.GetAsync($"/api/doctors/{doctorId}", o.Admin)).Data.GetProperty("timeOffs").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await o.Api.DeleteAsync($"/api/doctors/time-off/{added.Data.GetInt64()}", o.Admin)).Status);
    }

    [Fact]
    public async Task Changing_a_price_keeps_history_and_leaves_one_current_price()
    {
        var o = await OrgAsync();
        await using var _ = o.Api;
        var branchId = await AddBranchAsync(o, "NASR");
        var serviceId = await AddServiceAsync(o, await AddSpecialtyAsync(o));

        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync("/api/services/price", o.Admin, new { serviceId, branchId, price = 300.50m })).Status);
        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync("/api/services/price", o.Admin, new { serviceId, branchId, price = 300.50m })).Status);   // same price: no new row
        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync("/api/services/price", o.Admin, new { serviceId, branchId, price = 350m })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.PutAsync("/api/services/price", o.Admin, new { serviceId, branchId, price = -1 })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.PutAsync("/api/services/price", o.Admin, new { serviceId, branchId, price = 10.123m })).Status);

        var view = await o.Api.GetAsync($"/api/services/{serviceId}", o.Admin);
        var prices = view.Data.GetProperty("prices");
        Assert.Equal(1, prices.GetArrayLength());
        Assert.Equal(350m, prices[0].GetProperty("price").GetDecimal());

        await using var clinic = _db.CreateClinicContext(o.TenantId);
        var history = await clinic.ServicePrices.Where(p => p.ServiceId == serviceId).OrderBy(p => p.EffectiveFrom).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([300.50m, 350m], history.Select(p => p.Price).ToArray());
        Assert.NotNull(history[0].EffectiveTo);
        Assert.Null(history[1].EffectiveTo);
    }

    [Fact]
    public async Task A_member_without_the_permission_cannot_manage_clinic_data()
    {
        var o = await OrgAsync();
        await using var _ = o.Api;
        var roles = await o.Api.GetAsync("/api/roles", o.Admin);
        var nurse = roles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Nurse").GetProperty("id").GetInt64();
        await o.Api.PostAsync("/api/staff", o.Admin, new { fullName = "Nurse", email = "nurse@al-noor.test", password = ApiTestHost.Password, roleIds = new[] { nurse }, allBranches = true });
        var token = await o.Api.TokenAsync("nurse@al-noor.test", ApiTestHost.Password, "al-noor");

        Assert.Equal(HttpStatusCode.Forbidden, (await o.Api.PostAsync("/api/branches", token, new { name = "X", code = "X" })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await o.Api.PostAsync("/api/specialties", token, new { nameEn = "X", nameAr = "س" })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await o.Api.PostAsync("/api/doctors/search", token, new { })).Status);
    }
}
