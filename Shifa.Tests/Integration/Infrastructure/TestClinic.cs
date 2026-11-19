using System.Net;
using System.Text.Json;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace Shifa.Tests.Integration.Infrastructure;

/// <summary>
/// A ready clinic for the Phase 6–10 integration tests: one branch, a doctor working every day 09:00–13:00 (15-minute
/// grid), a 15-minute service priced at the branch, and a patient with a phone (portal access). Runs on a fake clock that
/// starts on Sunday 6 January 2030, 08:00 Cairo time.
/// </summary>
internal sealed class TestClinic : IAsyncDisposable
{
    public static readonly DateTimeOffset Sunday0800 = new(2030, 1, 6, 6, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Monday = new(2030, 1, 7);
    public const string PatientPhone = "+201112223334";

    private TestClinic(ApiTestHost api, FakeTimeProvider clock, string slug)
    {
        Api = api;
        Clock = clock;
        Slug = slug;
    }

    public ApiTestHost Api { get; }
    public FakeTimeProvider Clock { get; }
    public string Slug { get; }
    public string Platform { get; private set; } = "";
    public string Admin { get; private set; } = "";
    public long TenantId { get; private set; }
    public long BranchId { get; private set; }
    public long DoctorId { get; private set; }
    public long ServiceId { get; private set; }
    public long PatientId { get; private set; }

    public static async Task<TestClinic> CreateAsync(SqlServerFixture db, string slug = "test-clinic", decimal price = 300, int trialDays = 14)
    {
        var clock = new FakeTimeProvider(Sunday0800);
        var api = await ApiTestHost.StartAsync(db, clock);
        var c = new TestClinic(api, clock, slug) { Platform = await api.PlatformTokenAsync() };

        var plan = await api.PostAsync("/api/platform/plans", c.Platform,
            new { code = $"PLAN-{slug}", name = slug, price = 100m, billingPeriod = "Monthly", trialDays, entitlements = Array.Empty<object>() });
        c.TenantId = await api.CreateTenantAsync(c.Platform, Ok(plan).GetInt64(), slug);
        c.Admin = await api.AdminTokenAsync(slug);

        c.BranchId = Ok(await api.PostAsync("/api/branches", c.Admin, new { name = "Nasr City", code = "NASR" })).GetInt64();
        var specialtyId = Ok(await api.PostAsync("/api/specialties", c.Admin, new { nameEn = "General", nameAr = "عام" })).GetInt64();
        c.DoctorId = Ok(await api.PostAsync("/api/doctors", c.Admin,
            new { fullNameEn = "Dr. Synthetic", fullNameAr = "د. تجريبي", specialtyIds = new[] { specialtyId }, branchIds = new[] { c.BranchId } })).GetInt64();
        c.ServiceId = Ok(await api.PostAsync("/api/services", c.Admin,
            new { specialtyId, nameEn = "Consultation", nameAr = "كشف", durationMinutes = 15 })).GetInt64();
        Ok(await api.PutAsync("/api/services/price", c.Admin, new { serviceId = c.ServiceId, branchId = c.BranchId, price }));
        Ok(await api.PutAsync("/api/doctors/schedule", c.Admin, new
        {
            doctorId = c.DoctorId,
            branchId = c.BranchId,
            items = Enum.GetNames<DayOfWeek>().Select(d => new { dayOfWeek = d, startTime = "09:00:00", endTime = "13:00:00", slotMinutes = 15 }).ToArray(),
        }));
        c.PatientId = Ok(await api.PostAsync("/api/patients", c.Admin,
            new { fullName = "Synthetic Sara", dateOfBirth = "1990-05-01", gender = "Female", phoneNumber = PatientPhone })).GetInt64();
        return c;
    }

    /// <summary>The response's data, after checking the call succeeded.</summary>
    public static JsonElement Ok(ApiResult result)
    {
        Assert.True(result.Status == HttpStatusCode.OK, $"{result.Status}: {result.Body}");
        return result.Body.TryGetProperty("data", out var data) ? data : default;
    }

    public static Dictionary<string, string> Key(string? key = null) => new() { ["Idempotency-Key"] = key ?? Guid.NewGuid().ToString() };

    /// <summary>An extra invoice of <paramref name="quantity"/> services for the clinic's patient.</summary>
    public async Task<long> InvoiceAsync(int quantity = 1, long? patientId = null) =>
        Ok(await Api.PostAsync("/api/invoices", Admin,
            new { patientId = patientId ?? PatientId, branchId = BranchId, items = new[] { new { serviceId = ServiceId, quantity } } })).GetInt64();

    public async Task OpenShiftAsync() => Ok(await Api.PostAsync("/api/cashier-shifts", Admin, new { branchId = BranchId, openingCash = 0 }));

    public Task<ApiResult> PayAsync(long invoiceId, decimal amount, string? key = null) =>
        Api.CallAsync(HttpMethod.Post, "/api/payments", Admin, new { invoiceId, amount, method = "Cash" }, Key(key));

    public async Task<JsonElement> InvoiceViewAsync(long invoiceId) => Ok(await Api.GetAsync($"/api/invoices/{invoiceId}", Admin));

    public async Task<long> BookAsync(string localTime, DateOnly? day = null, long? patientId = null) =>
        Ok(await Api.CallAsync(HttpMethod.Post, "/api/appointments", Admin, new
        {
            patientId = patientId ?? PatientId,
            branchId = BranchId,
            doctorId = DoctorId,
            serviceId = ServiceId,
            startAt = $"{day ?? Monday:yyyy-MM-dd}T{localTime}:00+02:00",
            source = "Reception",
        }, Key())).GetInt64();

    /// <summary>Signs the patient's phone in to the portal with the OTP the fake SMS sender kept.</summary>
    public async Task<string> PortalTokenAsync()
    {
        Ok(await Api.PostAsync("/api/patients/portal-access", Admin, new { patientId = PatientId, phoneNumber = PatientPhone, relationship = "Self" }));
        Ok(await Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = Slug, phoneNumber = PatientPhone }));
        var code = Api.LastOtp(PatientPhone) ?? throw new InvalidOperationException("No OTP was sent.");
        return Ok(await Api.PostAsync("/api/portal/auth/otp/verify", null, new { tenantSlug = Slug, phoneNumber = PatientPhone, code }))
            .GetProperty("accessToken").GetString()!;
    }

    /// <summary>Turns the OnlinePayments feature on and saves the clinic's (fake) Paymob keys.</summary>
    public async Task EnableOnlinePaymentsAsync(string hmacSecret)
    {
        Ok(await Api.PutAsync("/api/platform/tenants/features", Platform, new { tenantId = TenantId, code = "Feature.OnlinePayments", enabled = true }));
        Ok(await Api.PutAsync("/api/payment-gateway", Admin,
            new { publicKey = "egy_pk_test_X", secretKey = "egy_sk_test_X", hmacSecret, integrationIds = new[] { 123456 } }));
    }

    /// <summary>Runs one outbox batch of a module now (the dispatcher's background loop is off in tests).</summary>
    public async Task DispatchOutboxAsync<TContext>() where TContext : ModuleDbContext
    {
        var dispatcher = Api.Services.GetServices<IHostedService>().OfType<OutboxDispatcher>().Single();
        while (await dispatcher.DispatchBatchAsync(typeof(TContext), TestContext.Current.CancellationToken) > 0)
        {
        }
    }

    public async Task<TJob> JobAsync<TJob>(Func<TJob, Task> run) where TJob : notnull
    {
        await using var scope = Api.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();
        await run(job);
        return job;
    }

    /// <summary>
    /// Moves the fake clock, forgets the cached tenant info (its 30-second cache runs on the real clock) and signs the
    /// admin in again (access tokens live 15 minutes on the fake clock).
    /// </summary>
    public async Task MoveClockToAsync(DateTimeOffset utc, bool signIn = true)
    {
        Clock.SetUtcNow(utc);
        Api.Services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>().Remove(global::Infrastructure.Repositories.Tenancy.TenancyApi.InfoCacheKey(TenantId));
        if (signIn)
            Admin = await Api.AdminTokenAsync(Slug);
    }

    public ValueTask DisposeAsync() => Api.DisposeAsync();
}
