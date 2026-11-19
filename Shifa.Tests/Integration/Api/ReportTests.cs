using System.Text.Json;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>Report numbers by branch-local day (ADR-016): billed vs collected, and a payment after local midnight.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class ReportTests
{
    private readonly SqlServerFixture _db;

    public ReportTests(SqlServerFixture db)
    {
        _db = db;
    }

    private static async Task<JsonElement> RevenueDayAsync(TestClinic c, string day) =>
        TestClinic.Ok(await c.Api.PostAsync("/api/reports/revenue", c.Admin, new { dateFrom = day, dateTo = day })).GetProperty("totals");

    [Fact]
    public async Task Revenue_report_counts_money_on_the_branch_local_day()
    {
        await using var c = await TestClinic.CreateAsync(_db);           // Sunday 08:00 Cairo
        var invoiceId = await c.InvoiceAsync();                           // 300
        await c.OpenShiftAsync();
        TestClinic.Ok(await c.PayAsync(invoiceId, 120));

        // Monday 00:30 Cairo is still Sunday in UTC: it must count for Monday.
        await c.MoveClockToAsync(new DateTimeOffset(2030, 1, 6, 22, 30, 0, TimeSpan.Zero));
        TestClinic.Ok(await c.PayAsync(invoiceId, 50));

        var sunday = await RevenueDayAsync(c, "2030-01-06");
        Assert.Equal(300m, sunday.GetProperty("invoiced").GetDecimal());
        Assert.Equal(120m, sunday.GetProperty("cash").GetDecimal());
        Assert.Equal(180m, sunday.GetProperty("difference").GetDecimal());

        var monday = await RevenueDayAsync(c, "2030-01-07");
        Assert.Equal(0m, monday.GetProperty("invoiced").GetDecimal());
        Assert.Equal(50m, monday.GetProperty("collections").GetDecimal());

        var daily = TestClinic.Ok(await c.Api.PostAsync("/api/reports/daily-branches", c.Admin, new { dateFrom = "2030-01-06", dateTo = "2030-01-07" }));
        Assert.Equal(300m, daily.GetProperty("totals").GetProperty("revenue").GetDecimal());
        Assert.Equal(170m, daily.GetProperty("totals").GetProperty("collections").GetDecimal());
    }
}
