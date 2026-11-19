using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Billing.Paymob;

/// <summary>
/// Transaction callback HMAC (Paymob docs): HMAC-SHA512 with the clinic's HMAC secret over these values of the
/// transaction ("obj" of the POST body) concatenated in this exact order; hex, compared in constant time.
/// </summary>
public static class PaymobHmac
{
    private static readonly string[] Fields =
    [
        "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction", "id", "integration_id",
        "is_3d_secure", "is_auth", "is_capture", "is_refunded", "is_standalone_payment", "is_voided", "order.id", "owner",
        "pending", "source_data.pan", "source_data.sub_type", "source_data.type", "success",
    ];

    public static string Compute(JsonElement transaction, string secret)
    {
        var concatenated = new StringBuilder();
        foreach (var field in Fields)
            concatenated.Append(ValueOf(transaction, field));

        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(concatenated.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    public static bool Verify(JsonElement transaction, string secret, string? hmac)
    {
        if (string.IsNullOrWhiteSpace(hmac))
            return false;
        var expected = Encoding.ASCII.GetBytes(Compute(transaction, secret));
        var given = Encoding.ASCII.GetBytes(hmac.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expected, given);
    }

    /// <summary>The value as Paymob writes it: booleans "true"/"false", numbers as sent, missing or null as "".</summary>
    internal static string ValueOf(JsonElement element, string path)
    {
        foreach (var part in path.Split('.'))
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(part, out element))
                return "";
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => element.GetRawText(),
            _ => "",
        };
    }
}
