using Core.Common;
using Core.Models.Billing;

namespace Core.Interfaces.Billing;

public interface IInvoiceRepo
{
    Task<Response> Search(SearchInvoicesModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Create(CreateInvoiceModel model, CancellationToken cancellationToken);
    Task<Response> EnsureVisitInvoice(long appointmentId, CancellationToken cancellationToken);
    Task<Response> Void(long id, VoidInvoiceModel model, CancellationToken cancellationToken);
    /// <summary>Needs an Idempotency-Key (scoped to the caller): a retry returns the same credit note (ADR-017).</summary>
    Task<Response> AddCreditNote(AddCreditNoteModel model, string? idempotencyKey, CancellationToken cancellationToken);
}
