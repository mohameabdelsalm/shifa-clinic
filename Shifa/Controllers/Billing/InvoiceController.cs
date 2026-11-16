using Core.Common;
using Core.Interfaces.Billing;
using Core.Models.Billing;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Billing;

[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceRepo _repo;

    public InvoiceController(IInvoiceRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("search")]
    [Permission(Permissions.BillingRead)]
    public async Task<Response> Search(SearchInvoicesModel model, CancellationToken ct) => await _repo.Search(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.BillingRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    /// <summary>An extra invoice for additional services, priced from the branch price list.</summary>
    [HttpPost]
    [Permission(Permissions.BillingCollect)]
    public async Task<Response> Create(CreateInvoiceModel model, CancellationToken ct) => await _repo.Create(model, ct);

    /// <summary>Creates the visit invoice of a checked-in appointment if it is missing (normally done at check-in).</summary>
    [HttpPost("visit/{appointmentId}")]
    [Permission(Permissions.BillingCollect)]
    public async Task<Response> EnsureVisitInvoice(long appointmentId, CancellationToken ct) => await _repo.EnsureVisitInvoice(appointmentId, ct);

    [HttpPut("{id}/void")]
    [Permission(Permissions.BillingRefund)]
    public async Task<Response> Void(long id, VoidInvoiceModel model, CancellationToken ct) => await _repo.Void(id, model, ct);

    [HttpPost("credit-notes")]
    [Permission(Permissions.BillingRefund)]
    public async Task<Response> AddCreditNote(AddCreditNoteModel model, CancellationToken ct) => await _repo.AddCreditNote(model, ct);
}
