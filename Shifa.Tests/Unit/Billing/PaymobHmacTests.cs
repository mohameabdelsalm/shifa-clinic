using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Infrastructure.Services.Billing.Paymob;

namespace Shifa.Tests.Unit.Billing;

/// <summary>
/// Paymob transaction callback HMAC (ADR-013). The expected value is built here from the documented field list, written
/// out by hand, so a change in field order or value formatting in PaymobHmac fails this test.
/// </summary>
public class PaymobHmacTests
{
    private const string Secret = "TEST-HMAC-SECRET";

    // JSON keys deliberately not in the signing order: the order comes from the field list, not the JSON.
    private const string Transaction = """
        {
          "success": true, "id": 900000001, "pending": false, "amount_cents": 25050, "currency": "EGP",
          "created_at": "2026-11-16T10:00:00.000000", "error_occured": false, "has_parent_transaction": false,
          "integration_id": 123456, "is_3d_secure": true, "is_auth": false, "is_capture": false, "is_refunded": false,
          "is_standalone_payment": true, "is_voided": false, "owner": 4242,
          "order": { "id": 5550001, "merchant_order_id": "SHF123" },
          "source_data": { "type": "card", "sub_type": "MasterCard", "pan": "2346" }
        }
        """;

    // amount_cents, created_at, currency, error_occured, has_parent_transaction, id, integration_id, is_3d_secure,
    // is_auth, is_capture, is_refunded, is_standalone_payment, is_voided, order.id, owner, pending,
    // source_data.pan, source_data.sub_type, source_data.type, success
    private const string DocumentedConcatenation =
        "25050" + "2026-11-16T10:00:00.000000" + "EGP" + "false" + "false" + "900000001" + "123456" + "true" +
        "false" + "false" + "false" + "true" + "false" + "5550001" + "4242" + "false" +
        "2346" + "MasterCard" + "card" + "true";

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static string Expected(string concatenation) =>
        Convert.ToHexStringLower(HMACSHA512.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(concatenation)));

    [Fact]
    public void Signs_the_documented_fields_in_order_with_lowercase_booleans()
    {
        Assert.Equal(Expected(DocumentedConcatenation), PaymobHmac.Compute(Parse(Transaction), Secret));
    }

    [Fact]
    public void Verifies_its_own_signature_in_any_hex_case()
    {
        var hmac = Expected(DocumentedConcatenation);
        Assert.True(PaymobHmac.Verify(Parse(Transaction), Secret, hmac));
        Assert.True(PaymobHmac.Verify(Parse(Transaction), Secret, hmac.ToUpperInvariant()));
    }

    [Fact]
    public void A_changed_signed_field_wrong_secret_or_missing_hmac_is_rejected()
    {
        var hmac = Expected(DocumentedConcatenation);

        Assert.False(PaymobHmac.Verify(Parse(Transaction.Replace("25050", "100")), Secret, hmac));
        Assert.False(PaymobHmac.Verify(Parse(Transaction.Replace("\"success\": true", "\"success\": false")), Secret, hmac));
        Assert.False(PaymobHmac.Verify(Parse(Transaction), "OTHER-SECRET", hmac));
        Assert.False(PaymobHmac.Verify(Parse(Transaction), Secret, null));
        Assert.False(PaymobHmac.Verify(Parse(Transaction), Secret, ""));
    }

    [Fact]
    public void Merchant_order_id_is_not_signed_so_it_alone_can_not_identify_the_attempt()
    {
        // Why the webhook also requires the signed order.id to match the attempt (ADR-017).
        var hmac = Expected(DocumentedConcatenation);
        Assert.True(PaymobHmac.Verify(Parse(Transaction.Replace("SHF123", "SHF999")), Secret, hmac));
    }

    [Fact]
    public void Missing_or_null_fields_count_as_empty()
    {
        var withoutCard = Transaction.Replace("\"pan\": \"2346\"", "\"pan\": null");
        Assert.Equal(Expected(DocumentedConcatenation.Replace("2346", "")), PaymobHmac.Compute(Parse(withoutCard), Secret));
    }
}
