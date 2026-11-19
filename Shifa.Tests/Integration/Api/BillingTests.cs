using System.Net;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>Desk payments and credit notes (ADR-011, ADR-017): no overpayment under concurrency, idempotent retries.</summary>
[Collection(SqlServerCollection.Name)]
public sealed class BillingTests
{
    private readonly SqlServerFixture _db;

    public BillingTests(SqlServerFixture db)
    {
        _db = db;
    }

    [Fact]
    public async Task Parallel_payments_never_collect_more_than_the_invoice_total()
    {
        await using var c = await TestClinic.CreateAsync(_db);
        var invoiceId = await c.InvoiceAsync();          // 300
        await c.OpenShiftAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => c.PayAsync(invoiceId, 100)));

        var accepted = results.Count(r => r.Status == HttpStatusCode.OK);
        var invoice = await c.InvoiceViewAsync(invoiceId);
        Assert.InRange(accepted, 1, 3);
        Assert.Equal(100m * accepted, invoice.GetProperty("paidAmount").GetDecimal());
        Assert.All(results.Where(r => r.Status != HttpStatusCode.OK), r => Assert.Contains(r.Status, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict }));
    }

    [Fact]
    public async Task A_payment_retry_replays_and_a_reused_key_with_another_body_is_refused()
    {
        await using var c = await TestClinic.CreateAsync(_db);
        var invoiceId = await c.InvoiceAsync();
        await c.OpenShiftAsync();
        var key = Guid.NewGuid().ToString();

        var first = TestClinic.Ok(await c.PayAsync(invoiceId, 100, key)).GetInt64();
        Assert.Equal(first, TestClinic.Ok(await c.PayAsync(invoiceId, 100.00m, key)).GetInt64());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await c.PayAsync(invoiceId, 99, key)).Status);
        Assert.Equal(100m, (await c.InvoiceViewAsync(invoiceId)).GetProperty("paidAmount").GetDecimal());
    }

    [Fact]
    public async Task A_credit_note_needs_a_key_and_a_retry_credits_only_once()
    {
        await using var c = await TestClinic.CreateAsync(_db);
        var invoiceId = await c.InvoiceAsync();
        object Note() => new { invoiceId, amount = 40, reason = "Goodwill" };

        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PostAsync("/api/invoices/credit-notes", c.Admin, Note())).Status);

        var key = TestClinic.Key();
        var first = TestClinic.Ok(await c.Api.CallAsync(HttpMethod.Post, "/api/invoices/credit-notes", c.Admin, Note(), key)).GetInt64();
        var retry = TestClinic.Ok(await c.Api.CallAsync(HttpMethod.Post, "/api/invoices/credit-notes", c.Admin, Note(), key)).GetInt64();

        Assert.Equal(first, retry);
        var invoice = await c.InvoiceViewAsync(invoiceId);
        Assert.Equal(40m, invoice.GetProperty("creditedAmount").GetDecimal());
        Assert.Single(invoice.GetProperty("creditNotes").EnumerateArray());
    }
}
