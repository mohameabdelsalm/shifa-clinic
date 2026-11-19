using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Infrastructure.Services.Billing.Paymob;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>
/// Paymob online payments with the Development fake client (ADR-013, ADR-017): signed callbacks only, one payment per
/// transaction even when callbacks repeat in parallel, no replay against another attempt, and online refunds that can't
/// be paid twice (the fake answers "pending" for amounts ending in .13 piasters and "refused" for .14).
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class OnlinePaymentTests
{
    private const string Secret = "TEST-HMAC-SECRET";
    private readonly SqlServerFixture _db;

    public OnlinePaymentTests(SqlServerFixture db)
    {
        _db = db;
    }

    private sealed record Attempt(long Id, string Reference, long OrderId);

    private static async Task<(TestClinic Clinic, string Portal)> ClinicAsync(SqlServerFixture db)
    {
        var c = await TestClinic.CreateAsync(db, "paymob-clinic", price: 250.50m);
        await c.EnableOnlinePaymentsAsync(Secret);
        return (c, await c.PortalTokenAsync());
    }

    private static async Task<Attempt> StartAsync(TestClinic c, string portal, long invoiceId)
    {
        var checkout = TestClinic.Ok(await c.Api.PostAsync($"/api/portal/invoices/{invoiceId}/pay-online", portal, new { }));
        Assert.StartsWith("https://eg.checkout.paymob.com/?publicKey=egy_pk_test_X&clientSecret=", checkout.GetProperty("checkoutUrl").GetString());
        var row = TestClinic.Ok(await c.Api.PostAsync("/api/online-payments/search", c.Admin, new { invoiceId, pageNumber = 1, pageSize = 5 }))
            .GetProperty("items")[0];
        return new Attempt(row.GetProperty("id").GetInt64(), row.GetProperty("reference").GetString()!, long.Parse(row.GetProperty("gatewayOrderId").GetString()!));
    }

    private static JsonObject Transaction(Attempt attempt, long transactionId, long cents, bool success = true) => new()
    {
        ["id"] = transactionId, ["pending"] = false, ["amount_cents"] = cents, ["success"] = success, ["is_auth"] = false,
        ["is_capture"] = false, ["is_standalone_payment"] = true, ["is_voided"] = false, ["is_refunded"] = false,
        ["is_3d_secure"] = true, ["integration_id"] = 123456, ["has_parent_transaction"] = false, ["owner"] = 4242,
        ["created_at"] = "2030-01-06T08:00:00.000000", ["currency"] = "EGP", ["error_occured"] = false,
        ["order"] = new JsonObject { ["id"] = attempt.OrderId, ["merchant_order_id"] = attempt.Reference },
        ["source_data"] = new JsonObject { ["pan"] = "2346", ["sub_type"] = "MasterCard", ["type"] = "card" },
    };

    private static string Sign(JsonObject transaction) =>
        PaymobHmac.Compute(JsonDocument.Parse(transaction.ToJsonString()).RootElement, Secret);

    private static Task<ApiResult> CallbackAsync(TestClinic c, JsonObject transaction, string? hmac) =>
        c.Api.PostAsync($"/api/webhooks/paymob/{c.TenantId}?hmac={hmac}", null,
            new JsonObject { ["type"] = "TRANSACTION", ["obj"] = JsonNode.Parse(transaction.ToJsonString()) });

    private static async Task<List<JsonElement>> OnlinePaymentsAsync(TestClinic c, long invoiceId) =>
        (await c.InvoiceViewAsync(invoiceId)).GetProperty("payments").EnumerateArray()
        .Where(p => p.GetProperty("method").GetString() == "Online").ToList();

    [Fact]
    public async Task Only_a_signed_callback_records_the_payment_and_repeats_in_parallel_record_it_once()
    {
        var (c, portal) = await ClinicAsync(_db);
        await using var _ = c;
        var invoiceId = await c.InvoiceAsync();
        var attempt = await StartAsync(c, portal, invoiceId);
        var transaction = Transaction(attempt, 900000001, 25050);

        Assert.Equal(HttpStatusCode.Unauthorized, (await CallbackAsync(c, transaction, new string('a', 128))).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await CallbackAsync(c, transaction, null)).Status);
        Assert.Empty(await OnlinePaymentsAsync(c, invoiceId));

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => CallbackAsync(c, transaction, Sign(transaction))));

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.Status));
        var payment = Assert.Single(await OnlinePaymentsAsync(c, invoiceId));
        Assert.Equal("900000001", payment.GetProperty("reference").GetString());
        Assert.Equal("Paid", (await c.InvoiceViewAsync(invoiceId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_signed_transaction_replayed_against_another_attempt_is_ignored()
    {
        var (c, portal) = await ClinicAsync(_db);
        await using var _ = c;
        var paid = await StartAsync(c, portal, await c.InvoiceAsync());
        var transaction = Transaction(paid, 900000001, 25050);
        TestClinic.Ok(await CallbackAsync(c, transaction, Sign(transaction)));

        var otherInvoice = await c.InvoiceAsync(quantity: 2);   // another amount: without the order check it would become NotApplied (refundable)
        var other = await StartAsync(c, portal, otherInvoice);
        var replay = JsonNode.Parse(transaction.ToJsonString())!.AsObject();
        replay["order"]!["merchant_order_id"] = other.Reference;     // not a signed field: the HMAC still matches

        Assert.Equal(HttpStatusCode.OK, (await CallbackAsync(c, replay, Sign(transaction))).Status);
        Assert.Empty(await OnlinePaymentsAsync(c, otherInvoice));
        var row = TestClinic.Ok(await c.Api.PostAsync("/api/online-payments/search", c.Admin, new { invoiceId = otherInvoice, pageNumber = 1, pageSize = 5 }))
            .GetProperty("items")[0];
        Assert.Equal("Pending", row.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Online_refunds_are_never_sent_twice_whatever_the_gateway_answers()
    {
        var (c, portal) = await ClinicAsync(_db);
        await using var _ = c;
        var invoiceId = await c.InvoiceAsync();
        var transaction = Transaction(await StartAsync(c, portal, invoiceId), 900000001, 25050);
        TestClinic.Ok(await CallbackAsync(c, transaction, Sign(transaction)));
        var paymentId = (await OnlinePaymentsAsync(c, invoiceId))[0].GetProperty("id").GetInt64();
        Task<ApiResult> Refund(decimal amount, Dictionary<string, string> key) =>
            c.Api.CallAsync(HttpMethod.Post, "/api/payments/refunds", c.Admin, new { paymentId, amount, reason = "Synthetic" }, key);

        // Gateway "pending": recorded as Pending, the invoice doesn't reopen yet, a retry replays the same refund.
        var pendingKey = TestClinic.Key();
        var pending = await Refund(10.13m, pendingKey);
        Assert.Equal(202, pending.Body.GetProperty("status").GetInt32());
        Assert.Equal(pending.Data.GetInt64(), (await Refund(10.13m, pendingKey)).Data.GetInt64());
        var invoice = await c.InvoiceViewAsync(invoiceId);
        Assert.Equal("Pending", Assert.Single(invoice.GetProperty("refunds").EnumerateArray()).GetProperty("status").GetString());
        Assert.Equal(0m, invoice.GetProperty("balance").GetDecimal());

        // The pending amount stays reserved.
        Assert.Equal(HttpStatusCode.BadRequest, (await Refund(250.50m, TestClinic.Key())).Status);

        // Gateway "refused": 400, the refund is Failed and its reservation released.
        Assert.Equal(HttpStatusCode.BadRequest, (await Refund(5.14m, TestClinic.Key())).Status);

        // Success: Completed, the invoice reopens by exactly that amount.
        TestClinic.Ok(await Refund(240.37m, TestClinic.Key()));
        invoice = await c.InvoiceViewAsync(invoiceId);
        var statuses = invoice.GetProperty("refunds").EnumerateArray()
            .ToDictionary(r => r.GetProperty("amount").GetDecimal(), r => r.GetProperty("status").GetString());
        Assert.Equal("Pending", statuses[10.13m]);
        Assert.Equal("Failed", statuses[5.14m]);
        Assert.Equal("Completed", statuses[240.37m]);
        Assert.Equal(240.37m, invoice.GetProperty("balance").GetDecimal());
    }
}
