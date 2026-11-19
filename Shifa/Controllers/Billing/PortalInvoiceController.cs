using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Billing;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Billing;

/// <summary>Invoices for patients and guardians signed in with a phone OTP.</summary>
[ApiController]
[PortalUser]
[RequireFeature(FeatureCodes.Billing)]
[RequireFeature(FeatureCodes.PatientPortal)]
[Route("api/portal/invoices")]
public class PortalInvoiceController : ControllerBase
{
    private readonly IPortalInvoiceRepo _repo;
    private readonly IOnlinePaymentRepo _onlinePayments;

    public PortalInvoiceController(IPortalInvoiceRepo repo, IOnlinePaymentRepo onlinePayments)
    {
        _repo = repo;
        _onlinePayments = onlinePayments;
    }

    [HttpGet]
    public async Task<Response> GetMine(CancellationToken ct) => await _repo.GetMine(ct);

    /// <summary>Starts paying the invoice balance online (Paymob); open the returned checkout URL. The payment is recorded by the gateway callback.</summary>
    [HttpPost("{id:long}/pay-online")]
    [RequireFeature(FeatureCodes.OnlinePayments)]
    public async Task<Response> PayOnline(long id, CancellationToken ct) => await _onlinePayments.StartPortalPayment(id, ct);
}
