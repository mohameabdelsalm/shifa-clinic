using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Infrastructure.Services.Identity;
using Infrastructure.Services;

namespace Shifa.Tests.Integration.Infrastructure;

/// <summary>
/// The real API running in-process against the SQL Server test database (empty at the start of each test).
/// It seeds one Platform Admin; tests create plans and tenants through the Platform API like a real operator would.
/// All identities are synthetic.
/// </summary>
internal sealed class ApiTestHost : IAsyncDisposable
{
    public const string PlatformEmail = "platform.admin@shifa.test";
    public const string PlatformPassword = "Platform-Pass-123";
    public const string Password = "Clinic-Pass-12345";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private ApiTestHost(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public IServiceProvider Services => _factory.Services;

    /// <param name="clock">Replaces the app's TimeProvider (tokens, booking rules, audit stamps all follow it).</param>
    public static async Task<ApiTestHost> StartAsync(SqlServerFixture db, TimeProvider? clock = null)
    {
        await db.ResetAsync();

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Shifa", db.ConnectionString);
            builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789-abcdef");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");
            builder.UseSetting("RateLimiting:WebhookPermitLimit", "100000");
            // Background work runs only when a test asks for it (dispatcher/jobs are called directly), so tests stay
            // deterministic and fast.
            builder.UseSetting("Jobs:Enabled", "false");
            builder.UseSetting("Outbox:Enabled", "false");
            builder.UseSetting("Paymob:UseFake", "true");
            builder.UseSetting("Paymob:NotificationBaseUrl", "http://localhost");
            builder.UseSetting("FileStorage:RootPath", Path.Combine(Path.GetTempPath(), "shifa-test-files"));
            if (clock is not null)
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton(clock);
                });
            }
        });

        var host = new ApiTestHost(factory);
        await PlatformAdminSeeder.SeedAsync(host.Services, "Platform Admin", PlatformEmail, PlatformPassword);
        return host;
    }

    public async Task<ApiResult> CallAsync(HttpMethod method, string url, string? token = null, object? body = null,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
            request.Headers.Add(name, value);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        using var response = await _client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var json = string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        return new ApiResult(response.StatusCode, json);
    }

    public Task<ApiResult> GetAsync(string url, string? token) => CallAsync(HttpMethod.Get, url, token);
    public Task<ApiResult> PostAsync(string url, string? token, object body) => CallAsync(HttpMethod.Post, url, token, body);
    public Task<ApiResult> PutAsync(string url, string? token, object? body = null) => CallAsync(HttpMethod.Put, url, token, body);
    public Task<ApiResult> DeleteAsync(string url, string? token) => CallAsync(HttpMethod.Delete, url, token);

    public Task<ApiResult> LoginAsync(string identifier, string password, string? tenantSlug = null) =>
        PostAsync("/api/auth/login", null, new { identifier, password, tenantSlug });

    /// <summary>Signs in and returns the access token; fails the test if sign-in is rejected.</summary>
    public async Task<string> TokenAsync(string identifier, string password, string? tenantSlug = null)
    {
        var result = await LoginAsync(identifier, password, tenantSlug);
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetProperty("accessToken").GetString()!;
    }

    public Task<string> PlatformTokenAsync() => TokenAsync(PlatformEmail, PlatformPassword);

    /// <summary>The 6-digit code of the last SMS the fake sender "sent" to this phone number (null if none).</summary>
    public string? LastOtp(string phoneNumber)
    {
        var message = Services.GetRequiredService<InMemorySmsOutbox>().LastTo(phoneNumber)?.Message;
        var match = message is null ? null : System.Text.RegularExpressions.Regex.Match(message, @"\b\d{6}\b");
        return match is { Success: true } ? match.Value : null;
    }

    public int SmsCount(string phoneNumber) =>
        Services.GetRequiredService<InMemorySmsOutbox>().Messages.Count(m => m.PhoneNumber == phoneNumber);

    public async Task<long> CreatePlanAsync(string platformToken, string code, int? maxEmployees = null,
        int? maxBranches = null, int? maxDoctors = null)
    {
        var entitlements = new List<object>();
        if (maxEmployees is not null)
            entitlements.Add(new { code = "MaxEmployees", enabled = true, limitValue = maxEmployees });
        if (maxBranches is not null)
            entitlements.Add(new { code = "MaxBranches", enabled = true, limitValue = maxBranches });
        if (maxDoctors is not null)
            entitlements.Add(new { code = "MaxDoctors", enabled = true, limitValue = maxDoctors });

        var result = await PostAsync("/api/platform/plans", platformToken,
            new { code, name = code, price = 100m, billingPeriod = "Monthly", trialDays = 14, entitlements });
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetInt64();
    }

    /// <summary>Onboards a tenant through the Platform API and returns its id. Its admin is <c>admin@{slug}.test</c>.</summary>
    public async Task<long> CreateTenantAsync(string platformToken, long planId, string slug)
    {
        var result = await PostAsync("/api/platform/tenants", platformToken, new
        {
            name = slug,
            slug,
            planId,
            adminFullName = $"Admin of {slug}",
            adminEmail = AdminEmail(slug),
            adminPassword = Password,
        });
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetInt64();
    }

    public static string AdminEmail(string slug) => $"admin@{slug}.test";

    public Task<string> AdminTokenAsync(string slug) => TokenAsync(AdminEmail(slug), Password, slug);

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}

internal readonly record struct ApiResult(HttpStatusCode Status, JsonElement Body)
{
    public JsonElement Data => Body.GetProperty("data");
    public string Message => Body.GetProperty("message").GetString() ?? "";
}
