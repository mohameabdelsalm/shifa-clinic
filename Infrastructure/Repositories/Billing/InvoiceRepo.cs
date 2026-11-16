using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.DTO.Clinic;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Enums.Scheduling;
using Core.Interfaces.Billing;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Core.Interfaces.Scheduling;
using Core.Models.Billing;
using Infrastructure.Data;
using Infrastructure.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Billing;

/// <summary>
/// Invoices (ADR-011). Never edited after creation: corrections are credit notes, and an invoice with no money on it
/// can be voided. Prices always come from the branch price list. Branch scope applies (outside = 404).
/// </summary>
public class InvoiceRepo : IInvoiceRepo
{
    private readonly BillingDbContext _context;
    private readonly IMapper _mapper;
    private readonly InvoiceIssuer _issuer;
    private readonly BillingNumbers _numbers;
    private readonly IBillingApi _billing;
    private readonly IClinicApi _clinic;
    private readonly IPatientsApi _patients;
    private readonly ISchedulingApi _scheduling;
    private readonly IAccessContext _access;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public InvoiceRepo(BillingDbContext context, IMapper mapper, InvoiceIssuer issuer, BillingNumbers numbers, IBillingApi billing,
        IClinicApi clinic, IPatientsApi patients, ISchedulingApi scheduling, IAccessContext access, ICurrentUser currentUser,
        IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _issuer = issuer;
        _numbers = numbers;
        _billing = billing;
        _clinic = clinic;
        _patients = patients;
        _scheduling = scheduling;
        _access = access;
        _currentUser = currentUser;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Search(SearchInvoicesModel model, CancellationToken cancellationToken)
    {
        var query = SortInvoices(InScope(_context.Invoices.AsNoTracking()).Where(FilterInvoices(model)), model);
        var paged = await query.ProjectTo<InvoiceListDto>(_mapper.ConfigurationProvider).ToPagedListAsync(model, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var invoice = await InScope(_context.Invoices.AsNoTracking()).FirstOrDefaultAsync(i => i.Id == id, cancellationToken) ?? throw NotFound();

        var dto = _mapper.Map<InvoiceDto>(invoice);
        dto.Items = _mapper.Map<List<InvoiceItemDto>>(
            await _context.InvoiceItems.AsNoTracking().Where(i => i.InvoiceId == id).OrderBy(i => i.Id).ToListAsync(cancellationToken));
        var payments = await _context.Payments.AsNoTracking().Where(p => p.InvoiceId == id).OrderBy(p => p.PaidAt).ToListAsync(cancellationToken);
        dto.Payments = _mapper.Map<List<PaymentDto>>(payments);
        var paymentIds = payments.Select(p => p.Id).ToList();
        dto.Refunds = _mapper.Map<List<RefundDto>>(
            await _context.Refunds.AsNoTracking().Where(r => paymentIds.Contains(r.PaymentId)).OrderBy(r => r.Id).ToListAsync(cancellationToken));
        dto.CreditNotes = _mapper.Map<List<CreditNoteDto>>(
            await _context.CreditNotes.AsNoTracking().Where(c => c.InvoiceId == id).OrderBy(c => c.Id).ToListAsync(cancellationToken));

        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: dto);
    }

    public async Task<Response> Create(CreateInvoiceModel model, CancellationToken cancellationToken)
    {
        EnsureBranchInScope(model.BranchId);
        if (!await _patients.PatientExistsAsync(model.PatientId, cancellationToken))
            throw new BusinessException(_localizer["PatientNotFound"].Value, HttpStatusCode.NotFound);

        if (model.AppointmentId is long appointmentId)
        {
            var appointment = await _scheduling.GetAppointmentForVisitAsync(appointmentId, cancellationToken);
            if (appointment is null || appointment.PatientId != model.PatientId || appointment.BranchId != model.BranchId)
                throw new BusinessException(_localizer["AppointmentNotFound"].Value, HttpStatusCode.NotFound);
        }

        // Prices from the branch price list only (no typed prices, no discounts).
        var lines = new List<(ServicePriceSnapshot, int)>();
        foreach (var item in model.Items)
        {
            var price = await _clinic.GetCurrentPriceAsync(item.ServiceId, model.BranchId, cancellationToken)
                ?? throw new BusinessException(_localizer["ServiceHasNoPrice"].Value, HttpStatusCode.BadRequest);
            lines.Add((price, item.Quantity));
        }

        var invoice = await _issuer.IssueAsync(InvoiceKindEnum.Extra, model.PatientId, model.BranchId, model.AppointmentId, lines, cancellationToken);
        await Audit("Billing.InvoiceIssued", invoice.Id, invoice.Number);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: invoice.Id);
    }

    /// <summary>Repair path: creates the visit invoice of a checked-in appointment if the check-in didn't (idempotent).</summary>
    public async Task<Response> EnsureVisitInvoice(long appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await _scheduling.GetAppointmentForVisitAsync(appointmentId, cancellationToken);
        if (appointment is null || !_access.CanAccessBranch(appointment.BranchId))
            throw new BusinessException(_localizer["AppointmentNotFound"].Value, HttpStatusCode.NotFound);
        if (appointment.Status is not (AppointmentStatusEnum.CheckedIn or AppointmentStatusEnum.InProgress or AppointmentStatusEnum.Completed))
            throw new BusinessException(_localizer["AppointmentNotCheckedIn"].Value, HttpStatusCode.BadRequest);

        var invoiceId = await _billing.EnsureVisitInvoiceAsync(
            new VisitInvoiceRequest(appointment.Id, appointment.PatientId, appointment.BranchId, appointment.ServiceId), cancellationToken);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: invoiceId);
    }

    public async Task<Response> Void(long id, VoidInvoiceModel model, CancellationToken cancellationToken)
    {
        var invoice = await InScope(_context.Invoices).FirstOrDefaultAsync(i => i.Id == id, cancellationToken) ?? throw NotFound();
        if (!invoice.TryVoid(model.Reason.Trim(), _time.GetUtcNow()))
            throw new BusinessException(_localizer["InvoiceCantBeVoided"].Value, HttpStatusCode.BadRequest);

        await _context.SaveChangesAsync(cancellationToken);   // rowversion: a payment racing the void → 409
        await Audit("Billing.InvoiceVoided", invoice.Id, model.Reason.Trim());
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> AddCreditNote(AddCreditNoteModel model, CancellationToken cancellationToken)
    {
        var invoice = await InScope(_context.Invoices).FirstOrDefaultAsync(i => i.Id == model.InvoiceId, cancellationToken) ?? throw NotFound();
        if (invoice.Status == InvoiceStatusEnum.Void)
            throw new BusinessException(_localizer["InvoiceVoid"].Value, HttpStatusCode.BadRequest);
        if (!invoice.TryApplyCredit(model.Amount))
            throw new BusinessException(_localizer["AmountExceedsBalance"].Value, HttpStatusCode.BadRequest);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var (year, sequence, number) = await _numbers.NextAsync("CN",
            y => _context.CreditNotes.Where(c => c.Year == y).MaxAsync(c => (int?)c.Sequence, cancellationToken), cancellationToken);
        var note = new CreditNote
        {
            InvoiceId = invoice.Id,
            Number = number,
            Year = year,
            Sequence = sequence,
            Amount = model.Amount,
            Reason = model.Reason.Trim(),
            CreatedByUserId = _currentUser.GetUserId() ?? throw new InvalidOperationException("No signed-in user."),
        };
        _context.CreditNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);   // the invoice update is in this save (rowversion)
        await transaction.CommitAsync(cancellationToken);

        await Audit("Billing.CreditNoteAdded", invoice.Id, $"{number} {model.Amount}");
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: note.Id);
    }

    private static Expression<Func<Invoice, bool>> FilterInvoices(SearchInvoicesModel model)
    {
        var text = model.SearchText;
        DateTimeOffset? fromUtc = model.FromDate is DateOnly from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        DateTimeOffset? toUtc = model.ToDate is DateOnly to ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        return i => (text == null || i.Number.Contains(text))
                    && (model.BranchId == null || i.BranchId == model.BranchId)
                    && (model.PatientId == null || i.PatientId == model.PatientId)
                    && (model.AppointmentId == null || i.AppointmentId == model.AppointmentId)
                    && (model.Status == null || i.Status == model.Status)
                    && (model.Kind == null || i.Kind == model.Kind)
                    && (fromUtc == null || i.IssuedAt >= fromUtc)
                    && (toUtc == null || i.IssuedAt < toUtc);
    }

    private static IQueryable<Invoice> SortInvoices(IQueryable<Invoice> query, SearchInvoicesModel model) => model.SortBy switch
    {
        "number" => query.OrderBy(model.SortType, i => i.Number).ThenBy(i => i.Id),
        "total" => query.OrderBy(model.SortType, i => i.Total).ThenBy(i => i.Id),
        "balance" => query.OrderBy(model.SortType, i => i.Total - i.CreditedAmount - i.PaidAmount).ThenBy(i => i.Id),
        "status" => query.OrderBy(model.SortType, i => i.Status).ThenBy(i => i.Id),
        _ => query.OrderBy(model.SortType, i => i.IssuedAt).ThenBy(i => i.Id),
    };

    private IQueryable<Invoice> InScope(IQueryable<Invoice> invoices)
    {
        if (_access.HasAllBranches)
            return invoices;
        var scope = _access.BranchIds.ToList();
        return invoices.Where(i => scope.Contains(i.BranchId));
    }

    private void EnsureBranchInScope(long branchId)
    {
        if (!_access.CanAccessBranch(branchId))
            throw new BusinessException(_localizer["BranchNotFound"].Value, HttpStatusCode.NotFound);
    }

    private Task Audit(string action, long invoiceId, string? detail) =>
        _audit.LogAsync(action, "Invoice", invoiceId, detail: detail, cancellationToken: CancellationToken.None);

    private BusinessException NotFound() => new(_localizer["InvoiceNotFound"].Value, HttpStatusCode.NotFound);
}
