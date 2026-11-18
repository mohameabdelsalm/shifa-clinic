using System.Text.Json;

namespace Infrastructure.Services.Billing.Paymob;

/// <summary>The fields of a Paymob transaction callback ("obj") that Shifa acts on.</summary>
public sealed record PaymobTransaction(
    string? TransactionId,
    string? MerchantOrderId,
    string? OrderId,
    long AmountCents,
    string Currency,
    bool Success,
    bool Pending,
    bool IsRefundOrVoid)
{
    public static PaymobTransaction From(JsonElement obj)
    {
        string Text(string path) => PaymobHmac.ValueOf(obj, path);
        bool Flag(string path) => Text(path) == "true";

        var merchantOrderId = Text("order.merchant_order_id");
        return new PaymobTransaction(
            TransactionId: Text("id") is { Length: > 0 } id ? id : null,
            MerchantOrderId: merchantOrderId.Length > 0 ? merchantOrderId : null,
            OrderId: Text("order.id") is { Length: > 0 } orderId ? orderId : null,
            AmountCents: long.TryParse(Text("amount_cents"), out var cents) ? cents : -1,
            Currency: Text("currency"),
            Success: Flag("success"),
            Pending: Flag("pending"),
            IsRefundOrVoid: Flag("is_refunded") || Flag("is_voided") || Flag("has_parent_transaction") || Flag("is_refund") || Flag("is_void"));
    }
}
