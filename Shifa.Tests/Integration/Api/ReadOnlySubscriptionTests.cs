using System.Net;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>A6 (ADR-014): after the subscription ends the clinic is read-only for 30 days, then nobody can sign in.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class ReadOnlySubscriptionTests
{
    private readonly SqlServerFixture _db;

    public ReadOnlySubscriptionTests(SqlServerFixture db)
    {
        _db = db;
    }

    [Fact]
    public async Task An_ended_trial_allows_reads_refuses_changes_and_a_payment_restores_writes()
    {
        await using var c = await TestClinic.CreateAsync(_db);           // 14-day trial
        await c.MoveClockToAsync(TestClinic.Sunday0800.AddDays(15));

        Assert.Equal(HttpStatusCode.OK, (await c.Api.GetAsync("/api/branches/all", c.Admin)).Status);
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/patients/search", c.Admin, new { pageNumber = 1, pageSize = 5 })).Status);
        Assert.True(TestClinic.Ok(await c.Api.GetAsync("/api/subscription", c.Admin)).GetProperty("isReadOnly").GetBoolean());

        var refused = await c.Api.PostAsync("/api/branches", c.Admin, new { name = "New", code = "NEW" });
        Assert.Equal(HttpStatusCode.Forbidden, refused.Status);
        Assert.Contains("read-only", refused.Message);

        TestClinic.Ok(await c.Api.PostAsync("/api/platform/tenants/payments", await c.Api.PlatformTokenAsync(), new { tenantId = c.TenantId, amount = 100, reference = "RENEW" }));
        await c.MoveClockToAsync(c.Clock.GetUtcNow());
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/branches", c.Admin, new { name = "New", code = "NEW" })).Status);
    }

    [Fact]
    public async Task Thirty_days_after_the_end_nobody_can_sign_in()
    {
        await using var c = await TestClinic.CreateAsync(_db);
        await c.MoveClockToAsync(TestClinic.Sunday0800.AddDays(14 + 31), signIn: false);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.Api.LoginAsync(ApiTestHost.AdminEmail(c.Slug), ApiTestHost.Password, c.Slug)).Status);
    }
}
