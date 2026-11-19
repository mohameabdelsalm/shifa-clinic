using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Core.Models.Billing;
using Infrastructure.Data;
using Infrastructure.Services.Billing.Paymob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Repositories.Billing;

/// <summary>
/// Online payments with Paymob (ADR-013). Starting a payment never records money: only the signed callback does
/// (<see cref="PaymobWebhookHandler"/>).
/// </summary>
public class OnlinePaymentRepo : IOnlinePaymentRepo
{
    private const string FallbackEmail = "na@example.com";

    private readonly BillingDbContext _context;
    private readonly PaymentGatewaySecrets _secrets;
    private readonly IPaymobClient _client;
    private readonly PaymobOptions _options;
    private readonly IPatientsApi _patients;
    private readonly IAccessContext _access;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public OnlinePaymentRepo(BillingDbContext context, PaymentGatewaySecrets secrets, IPaymobClient client, IOptions<PaymobOptions> options,
        IPatientsApi patients, IAccessContext access, ITenantContext tenant, IMapper mapper, IAuditService audit, TimeProvider time,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _secrets = secrets;
        _client = client;
        _options = options.Value;
        _patients = patients;
        _access = access;
        _tenant = tenant;
        _mapper = mapper;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> StartPortalPayment(long invoiceId, CancellationToken cancellationToken)
    {
        var phone = _access.PortalPhoneNumber ?? throw new InvalidOperationException("Not a portal request.");
        var patientIds = (await _patients.GetPortalPatientsAsync(phone, cancellationToken)).Select(p => p.PatientId).ToList();

        var invoice = await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId && patientIds.Contains(i.PatientId), cancellationToken)
            ?? throw new BusinessException(_localizer["InvoiceNotFound"].Value, HttpStatusCode.NotFound);
        if (invoice.Status == InvoiceStatusEnum.Void)
            throw new BusinessException(_localizer["InvoiceVoid"].Value, HttpStatusCode.BadRequest);
        if (invoice.Balance <= 0)
            throw new BusinessException(_localizer["NothingToPay"].Value, HttpStatusCode.BadRequest);

        var credentials = await _secrets.LoadAsync(cancellationToken)
            ?? throw new BusinessException(_localizer["OnlinePaymentsNotConfigured"].Value, HttpStatusCode.BadRequest);
        var notificationBase = string.IsNullOrWhiteSpace(_options.NotificationBaseUrl)
            ? throw new InvalidOperationException("Paymob:NotificationBaseUrl is not configured.")
            : _options.NotificationBaseUrl.TrimEnd('/');
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("No tenant.");

        var attempt = new OnlinePaymentAttempt
        {
            InvoiceId = invoice.Id,
            PatientId = invoice.PatientId,
            Amount = invoice.Balance,
            CurrencyCode = invoice.CurrencyCode,
            Reference = $"SHF{Guid.NewGuid():N}",
        };
        _context.OnlinePaymentAttempts.Add(attempt);
        // Saved before calling Paymob, so a callback can always find it.
        await _context.SaveChangesAsync(cancellationToken);

        var contact = await _patients.GetBillingContactAsync(invoice.PatientId, cancellationToken);
        var (firstName, lastName) = SplitName(contact?.FullName);

        PaymobIntention intention;
        try
        {
            intention = await _client.CreateIntentionAsync(credentials, new PaymobIntentionRequest(
                PaymobAmounts.ToCents(attempt.Amount),
                attempt.CurrencyCode,
                attempt.Reference,
                $"Invoice {invoice.Number}",
                firstName,
                lastName,
                string.IsNullOrWhiteSpace(contact?.Email) ? FallbackEmail : contact.Email,
                phone,
                $"{notificationBase}/api/webhooks/paymob/{tenantId}",
                string.IsNullOrWhiteSpace(_options.RedirectionUrl) ? null : _options.RedirectionUrl,
                _options.IntentionExpirySeconds), cancellationToken);
        }
        catch (PaymobException)
        {
            attempt.Status = OnlineAttemptStatusEnum.Failed;
            attempt.CompletedAt = _time.GetUtcNow();
            await _context.SaveChangesAsync(CancellationToken.None);
            throw new BusinessException(_localizer["PaymentGatewayError"].Value, HttpStatusCode.BadGateway);
        }

        attempt.GatewayIntentionId = intention.Id;
        attempt.GatewayOrderId = intention.OrderId;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Billing.OnlinePaymentStarted", nameof(OnlinePaymentAttempt), attempt.Id,
            detail: $"invoice {invoice.Number} {attempt.Amount}", cancellationToken: CancellationToken.None);

        var checkoutUrl = $"{_options.CheckoutUrl.TrimEnd('/')}/?publicKey={Uri.EscapeDataString(credentials.PublicKey)}" +
                          $"&clientSecret={Uri.EscapeDataString(intention.ClientSecret)}";
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: new OnlineCheckoutDto
        {
            AttemptId = attempt.Id,
            CheckoutUrl = checkoutUrl,
            Amount = attempt.Amount,
            CurrencyCode = attempt.CurrencyCode,
        });
    }

    public async Task<Response> Search(SearchOnlinePaymentsModel model, CancellationToken cancellationToken)
    {
        var query = SortAttempts(InScope(_context.OnlinePaymentAttempts.AsNoTracking()).Where(FilterAttempts(model)), model);
        var paged = await query.ProjectTo<OnlinePaymentAttemptDto>(_mapper.ConfigurationProvider).ToPagedListAsync(model, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    private static Expression<Func<OnlinePaymentAttempt, bool>> FilterAttempts(SearchOnlinePaymentsModel model)
    {
        var text = model.SearchText;
        DateTimeOffset? fromUtc = model.FromDate is DateOnly from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        DateTimeOffset? toUtc = model.ToDate is DateOnly to ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        return a => (text == null || a.Reference.Contains(text) || (a.GatewayTransactionId != null && a.GatewayTransactionId.Contains(text)))
                    && (model.InvoiceId == null || a.InvoiceId == model.InvoiceId)
                    && (model.PatientId == null || a.PatientId == model.PatientId)
                    && (model.Status == null || a.Status == model.Status)
                    && (fromUtc == null || a.CreationDate >= fromUtc)
                    && (toUtc == null || a.CreationDate < toUtc);
    }

    private static IQueryable<OnlinePaymentAttempt> SortAttempts(IQueryable<OnlinePaymentAttempt> query, SearchOnlinePaymentsModel model) => model.SortBy switch
    {
        "amount" => query.OrderBy(model.SortType, a => a.Amount).ThenBy(a => a.Id),
        "status" => query.OrderBy(model.SortType, a => a.Status).ThenBy(a => a.Id),
        _ => query.OrderBy(model.SortType, a => a.CreationDate).ThenBy(a => a.Id),
    };

    public async Task<Response> RefundNotApplied(long attemptId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("No tenant.");
        var credentials = await _secrets.LoadAsync(cancellationToken)
            ?? throw new BusinessException(_localizer["OnlinePaymentsNotConfigured"].Value, HttpStatusCode.BadRequest);

        // 1) Reserve: NotApplied → Refunding in its own commit, under the callback's lock. A double click or a retry
        //    finds Refunding and stops; nothing can call the gateway twice for this attempt (ADR-017).
        OnlinePaymentAttempt attempt;
        await using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            attempt = await InScope(_context.OnlinePaymentAttempts).FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken)
                ?? throw new BusinessException(_localizer["OnlinePaymentNotFound"].Value, HttpStatusCode.NotFound);
            await _context.Database.AcquireAsync(PaymobWebhookHandler.OnlineAttemptLock(tenantId, attempt.Id), cancellationToken);
            await _context.Entry(attempt).ReloadAsync(cancellationToken);
            if (attempt.Status != OnlineAttemptStatusEnum.NotApplied || attempt.GatewayTransactionId is null)
                throw new BusinessException(_localizer["OnlinePaymentNotRefundable"].Value, HttpStatusCode.BadRequest);

            // The same gateway transaction recorded as a payment on an invoice must be refunded there, not here.
            var transactionId = attempt.GatewayTransactionId;
            if (await _context.Payments.AnyAsync(p => p.Method == PaymentMethodEnum.Online && p.Reference == transactionId, cancellationToken))
                throw new BusinessException(_localizer["OnlinePaymentNotRefundable"].Value, HttpStatusCode.BadRequest);

            attempt.Status = OnlineAttemptStatusEnum.Refunding;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // 2) Gateway: the amount actually captured; the request's cancellation can't stop a refund half way.
        var amount = attempt.CapturedAmount ?? attempt.Amount;
        PaymobRefundResult? result = null;
        try
        {
            result = await _client.RefundAsync(credentials, attempt.GatewayTransactionId!, PaymobAmounts.ToCents(amount), CancellationToken.None);
        }
        catch (PaymobException)
        {
            // No answer: may have been refunded. Stays Refunding for a person to check in Paymob.
        }

        // 3) Record the outcome. Refused → back to NotApplied (can be tried again); unknown → stays Refunding.
        var outcome = result is null || result.Pending ? "unknown" : result.Success ? "refunded" : "refused";
        if (outcome != "unknown")
        {
            attempt.Status = outcome == "refunded" ? OnlineAttemptStatusEnum.Refunded : OnlineAttemptStatusEnum.NotApplied;
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        await _audit.LogAsync("Billing.OnlinePaymentReturn", nameof(OnlinePaymentAttempt), attempt.Id,
            detail: $"invoice {attempt.InvoiceId} {amount} {outcome} gateway refund {result?.TransactionId}", cancellationToken: CancellationToken.None);
        return outcome switch
        {
            "refunded" => new Response(_localizer["Updated"].Value, HttpStatusCode.OK),
            "refused" => throw new BusinessException(_localizer["OnlineRefundFailed"].Value, HttpStatusCode.BadRequest),
            _ => new Response(_localizer["RefundPending"].Value, HttpStatusCode.Accepted, data: attempt.Id),
        };
    }

    /// <summary>Staff see attempts of invoices in their branches only.</summary>
    private IQueryable<OnlinePaymentAttempt> InScope(IQueryable<OnlinePaymentAttempt> attempts)
    {
        if (_access.HasAllBranches)
            return attempts;
        var scope = _access.BranchIds.ToList();
        return attempts.Where(a => _context.Invoices.Any(i => i.Id == a.InvoiceId && scope.Contains(i.BranchId)));
    }

    private static (string First, string Last) SplitName(string? fullName)
    {
        var parts = (fullName ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => ("NA", "NA"),
            1 => (parts[0], "NA"),
            _ => (parts[0], parts[1]),
        };
    }
}
