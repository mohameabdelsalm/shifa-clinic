using System.Net;
using System.Security.Cryptography;
using System.Text;
using Core;
using Core.Common;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Common;
using Core.Models.Billing;
using Infrastructure.Configurations.Billing;
using Infrastructure.Data;
using Infrastructure.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Billing;

/// <summary>
/// Payments and refunds at the cash desk (ADR-011).
/// <list type="bullet">
/// <item>Idempotency-Key required, scoped to the caller: a retry returns the same payment/refund.</item>
/// <item>The amount is checked by the entities (no overpayment, no refund above what was paid).</item>
/// <item>The caller's open shift at the invoice's branch receives the money; payment, invoice and shift are updated in
/// one save, and the invoice and shift rowversions make concurrent operations serialize (409 → retry).</item>
/// </list>
/// </summary>
public class PaymentRepo : IPaymentRepo
{
    private const string PayOperation = "Pay";
    private const string RefundOperation = "Refund";
    private static readonly TimeSpan IdempotencyLifetime = TimeSpan.FromHours(24);

    private readonly BillingDbContext _context;
    private readonly BillingNumbers _numbers;
    private readonly IOnlineRefunds _onlineRefunds;
    private readonly IAccessContext _access;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PaymentRepo(BillingDbContext context, BillingNumbers numbers, IOnlineRefunds onlineRefunds, IAccessContext access,
        ICurrentUser currentUser, IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _numbers = numbers;
        _onlineRefunds = onlineRefunds;
        _access = access;
        _currentUser = currentUser;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Pay(AddPaymentModel model, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var key = RequireKey(idempotencyKey);
        var operation = ScopedOperation(PayOperation);
        var hash = Hash($"{model.InvoiceId}|{model.Amount}|{model.Method}|{model.Reference?.Trim()}");
        var now = _time.GetUtcNow();

        if (await FindReplayAsync(operation, key, hash, now, cancellationToken) is long replayed)
            return Created(replayed);

        var invoice = await InScope(_context.Invoices).FirstOrDefaultAsync(i => i.Id == model.InvoiceId, cancellationToken)
            ?? throw new BusinessException(_localizer["InvoiceNotFound"].Value, HttpStatusCode.NotFound);
        var shift = await OpenShiftAsync(invoice.BranchId, cancellationToken);

        if (invoice.Status == InvoiceStatusEnum.Void)
            throw new BusinessException(_localizer["InvoiceVoid"].Value, HttpStatusCode.BadRequest);
        if (!invoice.TryApplyPayment(model.Amount))
            throw new BusinessException(_localizer["AmountExceedsBalance"].Value, HttpStatusCode.BadRequest);

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var (year, sequence, number) = await _numbers.NextAsync("RCT",
                y => _context.Payments.Where(p => p.Year == y).MaxAsync(p => (int?)p.Sequence, cancellationToken), cancellationToken);

            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                CashierShiftId = shift.Id,
                Number = number,
                Year = year,
                Sequence = sequence,
                Amount = model.Amount,
                Method = model.Method,
                Reference = model.Reference?.Trim(),
                PaidAt = now,
                ReceivedByUserId = CurrentUserId(),
            };
            _context.Payments.Add(payment);
            TouchShift(shift);
            Remember(operation, key, hash, payment.Id, now);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await Audit("Billing.PaymentRecorded", "Payment", payment.Id, $"{number} invoice {invoice.Number} {model.Amount} {model.Method}");
            return Created(payment.Id);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException && IsIdempotencyConflict(ex))
        {
            // The same retry ran in parallel and committed first: answer with its result.
            _context.ChangeTracker.Clear();
            return Created(await FindReplayAsync(operation, key, hash, now, cancellationToken)
                           ?? throw new BusinessException(_localizer["IdempotencyKeyReused"].Value, HttpStatusCode.UnprocessableEntity));
        }
    }

    public async Task<Response> Refund(AddRefundModel model, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var key = RequireKey(idempotencyKey);
        var operation = ScopedOperation(RefundOperation);
        var hash = Hash($"{model.PaymentId}|{model.Amount}|{model.Reason.Trim()}");
        var now = _time.GetUtcNow();

        if (await FindReplayAsync(operation, key, hash, now, cancellationToken) is long replayed)
            return Created(replayed);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == model.PaymentId, cancellationToken)
            ?? throw new BusinessException(_localizer["PaymentNotFound"].Value, HttpStatusCode.NotFound);
        var invoice = await InScope(_context.Invoices).FirstOrDefaultAsync(i => i.Id == payment.InvoiceId, cancellationToken)
            ?? throw new BusinessException(_localizer["PaymentNotFound"].Value, HttpStatusCode.NotFound);

        // Online payments go back to the card through the gateway; cash-desk refunds come out of the refunder's drawer.
        var shift = payment.Method == PaymentMethodEnum.Online ? null : await OpenShiftAsync(invoice.BranchId, cancellationToken);

        if (!payment.TryRefund(model.Amount) || !invoice.TryApplyRefund(model.Amount))
            throw new BusinessException(_localizer["AmountExceedsRefundable"].Value, HttpStatusCode.BadRequest);

        string? externalReference = null;
        if (payment.Method == PaymentMethodEnum.Online)
            externalReference = await _onlineRefunds.RefundAsync(payment, model.Amount, cancellationToken);

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var (year, sequence, number) = await _numbers.NextAsync("RFD",
                y => _context.Refunds.Where(r => r.Year == y).MaxAsync(r => (int?)r.Sequence, cancellationToken), cancellationToken);

            var refund = new Refund
            {
                PaymentId = payment.Id,
                CashierShiftId = shift?.Id,
                Number = number,
                Year = year,
                Sequence = sequence,
                Amount = model.Amount,
                Method = payment.Method,
                Reason = model.Reason.Trim(),
                RefundedByUserId = CurrentUserId(),
                ExternalReference = externalReference,
            };
            _context.Refunds.Add(refund);
            if (shift is not null)
                TouchShift(shift);
            Remember(operation, key, hash, refund.Id, now);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await Audit("Billing.RefundRecorded", "Refund", refund.Id, $"{number} payment {payment.Number} {model.Amount} {payment.Method}");
            return Created(refund.Id);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException && IsIdempotencyConflict(ex))
        {
            _context.ChangeTracker.Clear();
            return Created(await FindReplayAsync(operation, key, hash, now, cancellationToken)
                           ?? throw new BusinessException(_localizer["IdempotencyKeyReused"].Value, HttpStatusCode.UnprocessableEntity));
        }
    }

    /// <summary>The caller's open shift at the branch (ADR-011: shifts are mandatory).</summary>
    private async Task<CashierShift> OpenShiftAsync(long branchId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return await _context.CashierShifts.FirstOrDefaultAsync(
                   s => s.BranchId == branchId && s.CashierUserId == userId && s.Status == ShiftStatusEnum.Open, cancellationToken)
               ?? throw new BusinessException(_localizer["NoOpenShift"].Value, HttpStatusCode.BadRequest);
    }

    /// <summary>UPDATE the shift row in the same save, so closing the shift can't race this payment/refund.</summary>
    private void TouchShift(CashierShift shift) => _context.Entry(shift).Property(s => s.OpeningCash).IsModified = true;

    private async Task<long?> FindReplayAsync(string operation, string key, string hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await _context.IdempotencyRecords.FirstOrDefaultAsync(r => r.Operation == operation && r.Key == key, cancellationToken);
        if (record is null)
            return null;
        if (record.ExpiresAt <= now)
        {
            _context.IdempotencyRecords.Remove(record);
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        return record.RequestHash == hash
            ? record.ResultId
            : throw new BusinessException(_localizer["IdempotencyKeyReused"].Value, HttpStatusCode.UnprocessableEntity);
    }

    private void Remember(string operation, string key, string hash, long resultId, DateTimeOffset now) =>
        _context.IdempotencyRecords.Add(new BillingIdempotencyRecord
        {
            Operation = operation,
            Key = key,
            RequestHash = hash,
            ResultId = resultId,
            ExpiresAt = now + IdempotencyLifetime,
        });

    private string RequireKey(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100
            ? throw new BusinessException(_localizer["IdempotencyKeyRequired"].Value, HttpStatusCode.BadRequest)
            : idempotencyKey.Trim();

    /// <summary>Keys are scoped to the staff membership, so one cashier's key can't block or replay another's.</summary>
    private string ScopedOperation(string operation) =>
        $"{operation}:m:{_access.MembershipId ?? throw new InvalidOperationException("Cash-desk operations need a staff membership.")}";

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool IsIdempotencyConflict(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains(BillingIndexes.Idempotency, StringComparison.Ordinal) == true;

    private IQueryable<Invoice> InScope(IQueryable<Invoice> invoices)
    {
        if (_access.HasAllBranches)
            return invoices;
        var scope = _access.BranchIds.ToList();
        return invoices.Where(i => scope.Contains(i.BranchId));
    }

    private long CurrentUserId() => _currentUser.GetUserId() ?? throw new InvalidOperationException("No signed-in user.");

    private Task Audit(string action, string entity, long id, string detail) =>
        _audit.LogAsync(action, entity, id, detail: detail, cancellationToken: CancellationToken.None);

    private Response Created(long id) => new(_localizer["Created"].Value, HttpStatusCode.OK, data: id);
}
