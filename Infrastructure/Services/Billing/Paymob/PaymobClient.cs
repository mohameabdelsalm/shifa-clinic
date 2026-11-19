using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Billing.Paymob;

/// <summary>Typed HttpClient for Paymob (base address = Paymob:BaseUrl). Logs status codes only, never bodies or keys.</summary>
public sealed class PaymobClient : IPaymobClient
{
    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;
    private readonly ILogger<PaymobClient> _logger;

    public PaymobClient(HttpClient http, ILogger<PaymobClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PaymobIntention> CreateIntentionAsync(PaymobCredentials credentials, PaymobIntentionRequest request, CancellationToken cancellationToken)
    {
        var body = new
        {
            amount = request.AmountCents,
            currency = request.Currency,
            payment_methods = credentials.IntegrationIds,
            items = new[] { new { name = request.ItemName, amount = request.AmountCents, description = request.ItemName, quantity = 1 } },
            billing_data = new
            {
                first_name = request.FirstName,
                last_name = request.LastName,
                email = request.Email,
                phone_number = request.PhoneNumber,
            },
            special_reference = request.Reference,
            notification_url = request.NotificationUrl,
            redirection_url = request.RedirectionUrl,
            expiration = request.ExpirySeconds,
        };

        using var json = await SendAsync("v1/intention/", body, credentials, "create intention", cancellationToken);
        var root = json.RootElement;
        if (!root.TryGetProperty("id", out var id) || !root.TryGetProperty("client_secret", out var secret) ||
            secret.GetString() is not { Length: > 0 } clientSecret)
            throw new PaymobException("Paymob create intention: unexpected response.");
        var orderId = root.TryGetProperty("intention_order_id", out var order) && order.ValueKind is JsonValueKind.Number or JsonValueKind.String
            ? order.ToString()
            : null;
        return new PaymobIntention(id.ToString(), clientSecret, orderId);
    }

    public async Task<PaymobRefundResult> RefundAsync(PaymobCredentials credentials, string transactionId, long amountCents, CancellationToken cancellationToken)
    {
        if (!long.TryParse(transactionId, out var transaction))
            throw new PaymobException("Paymob refund: the payment has no Paymob transaction id.");

        // A refused refund comes back as a 4xx with a transaction body (success = false): reported as a failed result.
        using var json = await SendAsync("api/acceptance/void_refund/refund", new { transaction_id = transaction, amount_cents = amountCents },
            credentials, "refund", cancellationToken, acceptClientErrors: true);
        var root = json.RootElement;
        var isObject = root.ValueKind == JsonValueKind.Object;
        var success = isObject && root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True;
        var pending = isObject && root.TryGetProperty("pending", out var p) && p.ValueKind == JsonValueKind.True;
        var id = isObject && root.TryGetProperty("id", out var i) ? i.ToString() : null;
        return new PaymobRefundResult(success && !pending, pending, id);
    }

    private async Task<JsonDocument> SendAsync(string path, object body, PaymobCredentials credentials, string operation, CancellationToken cancellationToken,
        bool acceptClientErrors = false)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: Json) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Token", credentials.SecretKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning("Paymob {Operation} failed: {Error}", operation, ex.GetType().Name);
            throw new PaymobException($"Paymob {operation}: no response.", ex);
        }

        using (response)
        {
            var status = (int)response.StatusCode;
            var refusedButAnswered = acceptClientErrors && status is >= 400 and < 500 and not 401 and not 403;
            if (!response.IsSuccessStatusCode && !refusedButAnswered)
            {
                _logger.LogWarning("Paymob {Operation} returned {StatusCode}", operation, status);
                throw new PaymobException($"Paymob {operation}: HTTP {status}.");
            }

            try
            {
                return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Paymob {Operation} returned {StatusCode} without JSON", operation, status);
                throw new PaymobException($"Paymob {operation}: response is not JSON.", ex);
            }
        }
    }
}
