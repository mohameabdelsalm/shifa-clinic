using System.Net;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>Phase 4: patient profiles for staff (registration, identifiers, guardians, consent, search) and tenant isolation.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class PatientsTests
{
    private const string Adult = "1990-05-01";

    private readonly SqlServerFixture _db;

    public PatientsTests(SqlServerFixture db)
    {
        _db = db;
    }

    private sealed record Org(ApiTestHost Api, string Admin);

    private async Task<Org> OrgAsync(string slug = "al-noor", ApiTestHost? existing = null)
    {
        var api = existing ?? await ApiTestHost.StartAsync(_db);
        var platform = await api.PlatformTokenAsync();
        var planId = await api.CreatePlanAsync(platform, $"PLAN-{slug}");
        await api.CreateTenantAsync(platform, planId, slug);
        return new Org(api, await api.AdminTokenAsync(slug));
    }

    private static Task<ApiResult> AddPatient(Org o, string name, string dob = Adult, string? phone = null,
        object[]? identifiers = null, object[]? guardians = null) =>
        o.Api.PostAsync("/api/patients", o.Admin, new
        {
            fullName = name,
            dateOfBirth = dob,
            gender = "Female",
            phoneNumber = phone,
            identifiers = identifiers ?? [],
            guardians = guardians ?? [],
        });

    private static object NationalId(string value) => new { type = "NationalId", value };

    private static async Task<long> AddPatientOkAsync(Org o, string name, string dob = Adult, string? phone = null, params object[] identifiers)
    {
        var result = await AddPatient(o, name, dob, phone, identifiers);
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetInt64();
    }

    [Fact]
    public async Task Patients_get_running_MRNs_per_tenant_and_duplicates_are_detected_by_identifier()
    {
        var a = await OrgAsync("al-noor");
        await using var host = a.Api;
        var b = await OrgAsync("al-shifa", a.Api);

        var first = await AddPatientOkAsync(a, "Mona Synthetic", identifiers: NationalId("29001010101010"));
        var second = await AddPatientOkAsync(a, "Sara Synthetic");
        var otherTenant = await AddPatientOkAsync(b, "Mona Synthetic", identifiers: NationalId("29001010101010"));   // same ID, different clinic: allowed

        var mrns = new[] { first, second }.Select(async id => (await a.Api.GetAsync($"/api/patients/{id}", a.Admin)).Data.GetProperty("mrn").GetString()).Select(t => t.Result).ToArray();
        Assert.Equal(["MRN-000001", "MRN-000002"], mrns);
        Assert.Equal("MRN-000001", (await b.Api.GetAsync($"/api/patients/{otherTenant}", b.Admin)).Data.GetProperty("mrn").GetString());

        var duplicate = await AddPatient(a, "Someone Else", identifiers: [NationalId("29001010101010")]);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.Status);
    }

    [Fact]
    public async Task Concurrent_registrations_all_succeed_with_distinct_MRNs()
    {
        var o = await OrgAsync();
        await using var host = o.Api;

        var results = await Task.WhenAll(Enumerable.Range(1, 8).Select(i => AddPatient(o, $"Patient {i}")));

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.Status));
        var search = await o.Api.PostAsync("/api/patients/search", o.Admin, new { pageNumber = 1, pageSize = 50 });
        var mrns = search.Data.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("mrn").GetString()).ToList();
        Assert.Equal(8, mrns.Distinct().Count());
    }

    [Fact]
    public async Task A_patient_under_18_needs_a_guardian_and_cannot_lose_the_last_one()
    {
        var o = await OrgAsync();
        await using var host = o.Api;
        var child = DateTime.UtcNow.AddYears(-5).ToString("yyyy-MM-dd");

        Assert.Equal(HttpStatusCode.BadRequest, (await AddPatient(o, "Child Synthetic", child)).Status);

        var created = await AddPatient(o, "Child Synthetic", child, guardians: [new { fullName = "Parent Synthetic", relationship = "Mother", phoneNumber = "01012345678" }]);
        Assert.Equal(HttpStatusCode.OK, created.Status);
        var id = created.Data.GetInt64();

        var view = await o.Api.GetAsync($"/api/patients/{id}", o.Admin);
        var guardian = view.Data.GetProperty("guardians")[0];
        Assert.Equal("+201012345678", guardian.GetProperty("phoneNumber").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.DeleteAsync($"/api/patients/guardians/{guardian.GetProperty("id").GetInt64()}", o.Admin)).Status);

        var second = await o.Api.PostAsync("/api/patients/guardians", o.Admin, new { patientId = id, fullName = "Father Synthetic", relationship = "Father" });
        Assert.Equal(HttpStatusCode.OK, second.Status);
        Assert.Equal(HttpStatusCode.OK, (await o.Api.DeleteAsync($"/api/patients/guardians/{guardian.GetProperty("id").GetInt64()}", o.Admin)).Status);
    }

    [Fact]
    public async Task Phone_numbers_are_normalized_and_invalid_input_is_rejected()
    {
        var o = await OrgAsync();
        await using var host = o.Api;

        var id = await AddPatientOkAsync(o, "Mona Synthetic", phone: "010 1234 5678");
        Assert.Equal("+201012345678", (await o.Api.GetAsync($"/api/patients/{id}", o.Admin)).Data.GetProperty("phoneNumber").GetString());

        // the reception can search with any spelling of the number
        foreach (var text in new[] { "01012345678", "+20 101 234 5678", "0020-1012345678" })
        {
            var search = await o.Api.PostAsync("/api/patients/search", o.Admin, new { search = text, pageNumber = 1, pageSize = 10 });
            Assert.Equal(1, search.Data.GetProperty("totalCount").GetInt32());
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await AddPatient(o, "Bad Phone", phone: "not-a-phone")).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await AddPatient(o, "Future", DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd"))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await AddPatient(o, "")).Status);
    }

    [Fact]
    public async Task Search_finds_patients_by_name_MRN_and_exact_identifier()
    {
        var o = await OrgAsync();
        await using var host = o.Api;
        await AddPatientOkAsync(o, "Mona Synthetic", identifiers: NationalId("29001010101010"));
        await AddPatientOkAsync(o, "Sara Synthetic");

        async Task<int> Count(string text) =>
            (await o.Api.PostAsync("/api/patients/search", o.Admin, new { search = text, pageNumber = 1, pageSize = 10 })).Data.GetProperty("totalCount").GetInt32();

        Assert.Equal(1, await Count("Mona"));
        Assert.Equal(2, await Count("Synthetic"));
        Assert.Equal(1, await Count("MRN-000002"));
        Assert.Equal(1, await Count("29001010101010"));
        Assert.Equal(0, await Count("2900101010"));   // identifiers match exactly, never partially
        Assert.Equal(0, await Count("nobody"));
    }

    [Fact]
    public async Task Updating_a_patient_replaces_identifiers_and_cannot_take_another_patients_identifier()
    {
        var o = await OrgAsync();
        await using var host = o.Api;
        var mona = await AddPatientOkAsync(o, "Mona Synthetic", identifiers: NationalId("29001010101010"));
        await AddPatientOkAsync(o, "Sara Synthetic", identifiers: NationalId("29505050505050"));

        object Update(object[] identifiers) => new { id = mona, fullName = "Mona S.", dateOfBirth = Adult, gender = "Female", identifiers };

        Assert.Equal(HttpStatusCode.BadRequest, (await o.Api.PutAsync("/api/patients", o.Admin, Update([NationalId("29505050505050")]))).Status);
        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync("/api/patients", o.Admin, Update([new { type = "Passport", value = "a1234567" }]))).Status);

        var view = await o.Api.GetAsync($"/api/patients/{mona}", o.Admin);
        var identifiers = view.Data.GetProperty("identifiers").EnumerateArray().Select(i => $"{i.GetProperty("type").GetString()}:{i.GetProperty("value").GetString()}").ToArray();
        Assert.Equal(["Passport:A1234567"], identifiers);   // old national ID removed, passport stored upper case

        // the freed national ID can be used by a new patient
        Assert.Equal(HttpStatusCode.OK, (await AddPatient(o, "New Person", identifiers: [NationalId("29001010101010")])).Status);
    }

    [Fact]
    public async Task Consent_can_be_granted_once_and_revoked()
    {
        var o = await OrgAsync();
        await using var host = o.Api;
        var id = await AddPatientOkAsync(o, "Mona Synthetic");

        var first = await o.Api.PostAsync("/api/patients/consents", o.Admin, new { patientId = id, consentType = "Treatment" });
        var again = await o.Api.PostAsync("/api/patients/consents", o.Admin, new { patientId = id, consentType = "Treatment" });
        Assert.Equal(first.Data.GetInt64(), again.Data.GetInt64());   // one active consent per type

        Assert.Equal(HttpStatusCode.OK, (await o.Api.PutAsync($"/api/patients/consents/{first.Data.GetInt64()}/revoke", o.Admin)).Status);
        var view = await o.Api.GetAsync($"/api/patients/{id}", o.Admin);
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, view.Data.GetProperty("consents")[0].GetProperty("revokedAt").ValueKind);

        // after a revoke, granting again creates a new record and keeps the history
        await o.Api.PostAsync("/api/patients/consents", o.Admin, new { patientId = id, consentType = "Treatment" });
        Assert.Equal(2, (await o.Api.GetAsync($"/api/patients/{id}", o.Admin)).Data.GetProperty("consents").GetArrayLength());
    }

    [Fact]
    public async Task Tenant_B_cannot_see_or_change_tenant_A_patients()
    {
        var a = await OrgAsync("al-noor");
        await using var host = a.Api;
        var b = await OrgAsync("al-shifa", a.Api);
        var id = await AddPatientOkAsync(a, "Mona Synthetic", phone: "01012345678", identifiers: NationalId("29001010101010"));
        var consent = (await a.Api.PostAsync("/api/patients/consents", a.Admin, new { patientId = id, consentType = "Treatment" })).Data.GetInt64();
        var access = (await a.Api.PostAsync("/api/patients/portal-access", a.Admin, new { patientId = id, phoneNumber = "01012345678", relationship = "Self" })).Data.GetInt64();
        var guardian = (await a.Api.PostAsync("/api/patients/guardians", a.Admin, new { patientId = id, fullName = "G", relationship = "Aunt" })).Data.GetInt64();

        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.GetAsync($"/api/patients/{id}", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync("/api/patients", b.Admin, new { id, fullName = "X", dateOfBirth = Adult, gender = "Male", identifiers = Array.Empty<object>() })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PostAsync("/api/patients/guardians", b.Admin, new { patientId = id, fullName = "X", relationship = "Y" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PostAsync("/api/patients/consents", b.Admin, new { patientId = id, consentType = "Marketing" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PostAsync("/api/patients/portal-access", b.Admin, new { patientId = id, phoneNumber = "01099999999", relationship = "Parent" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync($"/api/patients/consents/{consent}/revoke", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.DeleteAsync($"/api/patients/portal-access/{access}", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.DeleteAsync($"/api/patients/guardians/{guardian}", b.Admin)).Status);

        foreach (var text in new[] { "Mona", "MRN-000001", "29001010101010", "01012345678" })
            Assert.Equal(0, (await b.Api.PostAsync("/api/patients/search", b.Admin, new { search = text, pageNumber = 1, pageSize = 10 })).Data.GetProperty("totalCount").GetInt32());

        // untouched for its owner
        Assert.Equal("Mona Synthetic", (await a.Api.GetAsync($"/api/patients/{id}", a.Admin)).Data.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Staff_need_the_matching_patient_permission()
    {
        var o = await OrgAsync();
        await using var host = o.Api;
        var roles = await o.Api.GetAsync("/api/roles", o.Admin);
        long Role(string name) => roles.Data.EnumerateArray().Single(r => r.GetProperty("name").GetString() == name).GetProperty("id").GetInt64();

        async Task<string> Staff(string email, string role)
        {
            await o.Api.PostAsync("/api/staff", o.Admin, new { fullName = email, email, password = ApiTestHost.Password, roleIds = new[] { Role(role) }, allBranches = true });
            return await o.Api.TokenAsync(email, ApiTestHost.Password, "al-noor");
        }

        var id = await AddPatientOkAsync(o, "Mona Synthetic");
        var nurse = await Staff("nurse@al-noor.test", "Nurse");              // Patients.Read only
        var reception = await Staff("reception@al-noor.test", "Receptionist"); // Read, Create, Update
        var cashier = await Staff("cashier@al-noor.test", "Cashier");         // Patients.Read only

        Assert.Equal(HttpStatusCode.OK, (await o.Api.GetAsync($"/api/patients/{id}", nurse)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await AddAs(nurse)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await o.Api.PostAsync("/api/patients/consents", cashier, new { patientId = id, consentType = "Treatment" })).Status);
        Assert.Equal(HttpStatusCode.OK, (await AddAs(reception)).Status);

        Task<ApiResult> AddAs(string token) => o.Api.PostAsync("/api/patients", token, new { fullName = "New", dateOfBirth = Adult, gender = "Male" });
    }
}
