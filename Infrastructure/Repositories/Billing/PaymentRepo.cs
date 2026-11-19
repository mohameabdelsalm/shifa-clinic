using System.Net;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Events.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Common;
using Core.Models.Billing;
using Infrastructure.Data;
using Infrastructure.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Billing;

/// <summary>
/// Payments and refunds (ADR-011, ADR-017).
/// <list type="bullet">
/// <item>Idempotency-Key required, scoped to the caller: a retry returns the same payment/refund.</item>
/// <item>The amount is checked by the entities (no overpayment, no refund above what was paid).</item>
/// <item>Desk payments/refunds: the caller's open shift at the invoice's branch; payment, invoice and shift in one save, the
/// rowversions serialize concurrent operations (409 → retry).</item>
/// <item>Online refunds: reserve (Pending, own commit) → gateway → settle. A retry or a concurrent change can never send
/// money back twice; an unclear gateway answer stays Pending for a person to check.</item>
/// </list>
/// </summary>
public class PaymentRepo : IPaymentRepo
{
    private const string PayOperation = "Pay";
    private const string RefundOperation = "Refund";
    private const int SettleAttempts = 5;

    private readonly BillingDbContext _context;
    private readonly BillingNumbers _numbers;
    private readonly BillingIdempotency _idempotency;
    private readonly IOnlineRefunds _onlineRefunds;
    private readonly IAccessContext _access;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PaymentRepo(BillingDbContext context, BillingNumbers numbers, BillingIdempotency idempotency, IOnlineRefunds onlineRefunds,
        IAccessContext access, ICurrentUser currentUser, IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _numbers = numbers;
        _idempotency = idempotency;
        _onlineRefunds = onlineRefunds;
        _access = access;
        _currentUser = currentUser;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Pay(AddPaymentModel model, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var key = _idempotency.RequireKey(idempotencyKey);
        var operation = _idempotency.Operation(PayOperation);
        var hash = BillingIdempotency.Hash(model.InvoiceId, model.Amount, model.Method, model.Reference?.Trim());
        var now = _time.GetUtcNow();

        if (await _idempotency.FindReplayAsync(operation, key, hash, now, cancellationToken) is long replayed)
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
            _idempotency.Remember(operation, key, hash, payment.Id, now);
            _context.AddEvent(new PaymentRecorded(payment.Id, number, invoice.Id, invoice.Number, invoice.PatientId, invoice.BranchId,
                payment.Amount, invoice.CurrencyCode, payment.Method.ToString()), now);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await Audit("Billing.PaymentRecorded", "Payment", payment.Id, $"{number} invoice {invoice.Number} {model.Amount} {model.Method}");
            return Created(payment.Id);
        }
        catch (DbUpdateException ex) when (BillingIdempotency.IsConflict(ex))
        {
            // The same retry ran in parallel and committed first: answer with its result.
            return Created(await _idempotency.ReplayAfterConflictAsync(operation, key, hash, now, cancellationToken));
        }
    }

    public async Task<Response> Refund(AddRefundModel model, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new RefundRequest(
            _idempotency.RequireKey(idempotencyKey),
            _idempotency.Operation(RefundOperation),
            BillingIdempotency.Hash(model.PaymentId, model.Amount, model.Reason.Trim()),
            _time.GetUtcNow(),
            model);

        // A replay answers with the first result, also when that refund is still Pending.
        if (await _idempotency.FindReplayAsync(request.Operation, request.Key, request.Hash, request.Now, cancellationToken) is long replayed)
            return Created(replayed);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == model.PaymentId, cancellationToken)
            ?? throw new BusinessException(_localizer["PaymentNotFound"].Value, HttpStatusCode.NotFound);
        var invoice = await InScope(_context.Invoices).FirstOrDefaultAsync(i => i.Id == payment.InvoiceId, cancellationToken)
            ?? throw new BusinessException(_localizer["PaymentNotFound"].Value, HttpStatusCode.NotFound);

        return payment.Method == PaymentMethodEnum.Online
            ? await OnlineRefundAsync(request, payment, cancellationToken)
            : await DeskRefundAsync(request, payment, invoice, cancellationToken);
    }

    private sealed record RefundRequest(string Key, string Operation, string Hash, DateTimeOffset Now, AddRefundModel Model);

    /// <summary>Cash/card/wallet: out of the refunder's open shift, everything in one save.</summary>
    private async Task<Response> DeskRefundAsync(RefundRequest request, Payment payment, Invoice invoice, CancellationToken cancellationToken)
    {
        var shift = await OpenShiftAsync(invoice.BranchId, cancellationToken);
        if (!payment.TryRefund(request.Model.Amount) || !invoice.TryApplyRefund(request.Model.Amount))
            throw new BusinessException(_localizer["AmountExceedsRefundable"].Value, HttpStatusCode.BadRequest);

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var refund = await NewRefundAsync(payment, request, shift.Id, RefundStatusEnum.Completed, cancellationToken);
            TouchShift(shift);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await Audit("Billing.RefundRecorded", "Refund", refund.Id, $"{refund.Number} payment {payment.Number} {refund.Amount} {payment.Method}");
            return Created(refund.Id);
        }
        catch (DbUpdateException ex) when (BillingIdempotency.IsConflict(ex))
        {
            return Created(await _idempotency.ReplayAfterConflictAsync(request.Operation, request.Key, request.Hash, request.Now, cancellationToken));
        }
    }

    /// <summary>
    /// Online (ADR-017): 1) reserve the amount on the payment and save a Pending refund with the idempotency record, under
    /// a lock on the payment; 2) call the gateway, not cancellable by the client; 3) settle: Succeeded → Completed and the
    /// invoice reopens; Refused → Failed and the reservation is released; Unknown → stays Pending (visible, never resent).
    /// </summary>
    private async Task<Response> OnlineRefundAsync(RefundRequest request, Payment payment, CancellationToken cancellationToken)
    {
        Refund refund;
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.Database.AcquireAsync(RefundLock(payment), cancellationToken);
            if (await _idempotency.FindReplayAsync(request.Operation, request.Key, request.Hash, request.Now, cancellationToken) is long committed)
                return Created(committed);
            await _context.Entry(payment).ReloadAsync(cancellationToken);
            if (!payment.TryRefund(request.Model.Amount))
                throw new BusinessException(_localizer["AmountExceedsRefundable"].Value, HttpStatusCode.BadRequest);

            refund = await NewRefundAsync(payment, request, shiftId: null, RefundStatusEnum.Pending, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (BillingIdempotency.IsConflict(ex))
        {
            return Created(await _idempotency.ReplayAfterConflictAsync(request.Operation, request.Key, request.Hash, request.Now, cancellationToken));
        }

        var result = await _onlineRefunds.RefundAsync(payment, refund.Amount, CancellationToken.None);
        var settled = await SettleOnlineRefundAsync(refund.Id, payment, result);
        await Audit("Billing.OnlineRefund", "Refund", refund.Id,
            $"{refund.Number} payment {payment.Number} {refund.Amount} {result.Outcome} {(settled ? "" : "NOT SETTLED ")}gateway refund {result.Reference}");

        return result.Outcome switch
        {
            OnlineRefundOutcomeEnum.Succeeded when settled => Created(refund.Id),
            OnlineRefundOutcomeEnum.Refused when settled => throw new BusinessException(_localizer["OnlineRefundFailed"].Value, HttpStatusCode.BadRequest),
            _ => new Response(_localizer["RefundPending"].Value, HttpStatusCode.Accepted, data: refund.Id),
        };
    }

    /// <summary>
    /// Records the gateway's answer on the Pending refund. The money has (maybe) moved already, so this doesn't follow the
    /// request's cancellation and retries on concurrent changes with fresh data instead of failing. If it still can't be
    /// saved the refund stays Pending (reserved, never resent) and the audit has the gateway reference.
    /// </summary>
    private async Task<bool> SettleOnlineRefundAsync(long refundId, Payment payment, OnlineRefundResult result)
    {
        for (var attempt = 1; attempt <= SettleAttempts; attempt++)
        {
            _context.ChangeTracker.Clear();
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(CancellationToken.None);
                await _context.Database.AcquireAsync(RefundLock(payment), CancellationToken.None);
                var refund = await _context.Refunds.FirstAsync(r => r.Id == refundId, CancellationToken.None);
                if (refund.Status != RefundStatusEnum.Pending)
                    return true;

                refund.ExternalReference = result.Reference ?? refund.ExternalReference;
                if (result.Outcome == OnlineRefundOutcomeEnum.Succeeded)
                {
                    var invoice = await _context.Invoices.FirstAsync(i => i.Id == payment.InvoiceId, CancellationToken.None);
                    if (!invoice.TryApplyRefund(refund.Amount))
                        return false;
                    refund.Status = RefundStatusEnum.Completed;
                }
                else if (result.Outcome == OnlineRefundOutcomeEnum.Refused)
                {
                    var current = await _context.Payments.FirstAsync(p => p.Id == payment.Id, CancellationToken.None);
                    current.CancelRefund(refund.Amount);
                    refund.Status = RefundStatusEnum.Failed;
                }

                await _context.SaveChangesAsync(CancellationToken.None);
                await transaction.CommitAsync(CancellationToken.None);
                return true;
            }
            catch (Exception ex) when (ex is DbUpdateConcurrencyException or TimeoutException && attempt < SettleAttempts)
            {
                // The invoice changed meanwhile (desk payment, credit note): read it again and re-apply.
            }
            catch (Exception)
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>Numbers a refund (RFD-…) and adds it with the idempotency record; the caller saves inside its transaction.</summary>
    private async Task<Refund> NewRefundAsync(Payment payment, RefundRequest request, long? shiftId, RefundStatusEnum status,
        CancellationToken cancellationToken)
    {
        var (year, sequence, number) = await _numbers.NextAsync("RFD",
            y => _context.Refunds.Where(r => r.Year == y).MaxAsync(r => (int?)r.Sequence, cancellationToken), cancellationToken);
        var refund = new Refund
        {
            PaymentId = payment.Id,
            CashierShiftId = shiftId,
            Number = number,
            Year = year,
            Sequence = sequence,
            Amount = request.Model.Amount,
            Method = payment.Method,
            Reason = request.Model.Reason.Trim(),
            RefundedByUserId = CurrentUserId(),
            Status = status,
        };
        _context.Refunds.Add(refund);
        _idempotency.Remember(request.Operation, request.Key, request.Hash, refund.Id, request.Now);
        return refund;
    }

    private static string RefundLock(Payment payment) => $"billing:refund:{payment.TenantId}:{payment.Id}";

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
