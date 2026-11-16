using System.Text.Json;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Common;
using Infrastructure.Configurations.Billing;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Billing.Paymob;

/// <summary>
/// Paymob transaction callbacks (ADR-013). The URL's tenant id is only a hint: the body is trusted only after its HMAC
/// matches that clinic's secret. Then, under an app lock on the attempt:
/// <list type="bullet">
/// <item>successful + amount and currency match + the invoice still owes it → an Online payment (one save);</item>
/// <item>successful but no longer fits (paid at the desk meanwhile) → NotApplied, audited for a refund;</item>
/// <item>declined → Failed (a later success for the same attempt is still accepted: the patient may retry another card);</item>
/// <item>repeats, pending, refund/void callbacks and unknown references → ignored (200, so Paymob stops retrying).</item>
/// </list>
/// </summary>
public sealed class PaymobWebhookHandler : IPaymobWebhookHandler
{
    private const int MaxSaveAttempts = 3;

    private readonly ITenantScopeFactory _scopes;
    private readonly ILogger<PaymobWebhookHandler> _logger;

    public PaymobWebhookHandler(ITenantScopeFactory scopes, ILogger<PaymobWebhookHandler> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public Task<WebhookOutcome> HandleAsync(long tenantId, string? hmac, string body, CancellationToken cancellationToken) =>
        _scopes.RunAsync(tenantId, sp => HandleInTenantAsync(sp, tenantId, hmac, body, cancellationToken));

    private async Task<WebhookOutcome> HandleInTenantAsync(IServiceProvider services, long tenantId, string? hmac, string body,
        CancellationToken cancellationToken)
    {
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(body);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            _logger.LogWarning("Rejected a Paymob callback for tenant {TenantId}: body is not JSON.", tenantId);
            return WebhookOutcome.Rejected;
        }

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("obj", out var transaction) || transaction.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("Rejected a Paymob callback for tenant {TenantId}: no transaction object.", tenantId);
            return WebhookOutcome.Rejected;
        }

        var credentials = await services.GetRequiredService<PaymentGatewaySecrets>().LoadAsync(cancellationToken);
        if (credentials is null || !PaymobHmac.Verify(transaction, credentials.HmacSecret, hmac))
        {
            _logger.LogWarning("Rejected a Paymob callback for tenant {TenantId}: no gateway settings or invalid HMAC.", tenantId);
            return WebhookOutcome.Rejected;
        }

        var type = root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        if (!string.Equals(type, "TRANSACTION", StringComparison.OrdinalIgnoreCase))
            return WebhookOutcome.Accepted;

        var callback = PaymobTransaction.From(transaction);

        // Refunds are recorded when Shifa makes them; pending transactions get a final callback later.
        if (callback.IsRefundOrVoid || callback.Pending || callback.MerchantOrderId is null || callback.TransactionId is null)
            return WebhookOutcome.Accepted;

        var context = services.GetRequiredService<BillingDbContext>();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ApplyAsync(services, context, tenantId, callback, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxSaveAttempts)
            {
                // The invoice changed at the desk at the same moment: read it again and re-decide.
                context.ChangeTracker.Clear();
            }
            catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException &&
                                               ex.InnerException?.Message.Contains(BillingIndexes.OnlineTransaction, StringComparison.Ordinal) == true)
            {
                // This Paymob transaction is already recorded.
                context.ChangeTracker.Clear();
                return WebhookOutcome.Accepted;
            }
        }
    }

    private async Task<WebhookOutcome> ApplyAsync(IServiceProvider services, BillingDbContext context, long tenantId, PaymobTransaction callback,
        CancellationToken cancellationToken)
    {
        var audit = services.GetRequiredService<IAuditService>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var attempt = await context.OnlinePaymentAttempts.FirstOrDefaultAsync(a => a.Reference == callback.MerchantOrderId, cancellationToken);
        if (attempt is null)
        {
            _logger.LogWarning("Paymob callback for tenant {TenantId} has an unknown reference; ignored.", tenantId);
            return WebhookOutcome.Accepted;
        }

        await context.Database.AcquireAsync(OnlineAttemptLock(tenantId, attempt.Id), cancellationToken);
        await context.Entry(attempt).ReloadAsync(cancellationToken);
        if (attempt.Status is OnlineAttemptStatusEnum.Succeeded or OnlineAttemptStatusEnum.NotApplied or OnlineAttemptStatusEnum.Refunded)
            return WebhookOutcome.Accepted;

        if (!callback.Success)
        {
            if (attempt.Status == OnlineAttemptStatusEnum.Pending)
            {
                attempt.Status = OnlineAttemptStatusEnum.Failed;
                attempt.GatewayTransactionId = callback.TransactionId;
                attempt.CompletedAt = now;
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            return WebhookOutcome.Accepted;
        }

        attempt.GatewayTransactionId = callback.TransactionId;
        attempt.CompletedAt = now;

        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == attempt.InvoiceId, cancellationToken);
        var fits = callback.AmountCents == PaymobAmounts.ToCents(attempt.Amount)
                   && string.Equals(callback.Currency, attempt.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                   && invoice is not null
                   && invoice.Status != InvoiceStatusEnum.Void
                   && invoice.TryApplyPayment(attempt.Amount);

        if (!fits)
        {
            attempt.Status = OnlineAttemptStatusEnum.NotApplied;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await audit.LogAsync("Billing.OnlinePaymentNotApplied", nameof(OnlinePaymentAttempt), attempt.Id,
                detail: $"invoice {attempt.InvoiceId} transaction {callback.TransactionId} {callback.AmountCents} {callback.Currency}",
                cancellationToken: CancellationToken.None);
            return WebhookOutcome.Accepted;
        }

        var numbers = services.GetRequiredService<BillingNumbers>();
        var (year, sequence, number) = await numbers.NextAsync("RCT",
            y => context.Payments.Where(p => p.Year == y).MaxAsync(p => (int?)p.Sequence, cancellationToken), cancellationToken);

        var payment = new Payment
        {
            InvoiceId = invoice!.Id,
            CashierShiftId = null,
            Number = number,
            Year = year,
            Sequence = sequence,
            Amount = attempt.Amount,
            Method = PaymentMethodEnum.Online,
            Reference = callback.TransactionId,
            PaidAt = now,
            ReceivedByUserId = null,
        };
        context.Payments.Add(payment);
        attempt.Status = OnlineAttemptStatusEnum.Succeeded;
        attempt.PaymentId = payment.Id;

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await audit.LogAsync("Billing.OnlinePaymentRecorded", nameof(Payment), payment.Id,
            detail: $"{number} invoice {invoice.Number} {attempt.Amount} transaction {callback.TransactionId}", cancellationToken: CancellationToken.None);
        return WebhookOutcome.Accepted;
    }

    /// <summary>Shared with the NotApplied refund, so a callback and a refund of the same attempt never run together.</summary>
    public static string OnlineAttemptLock(long tenantId, long attemptId) => $"billing:online:{tenantId}:{attemptId}";
}
