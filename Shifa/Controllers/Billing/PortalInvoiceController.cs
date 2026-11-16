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

    public PortalInvoiceController(IPortalInvoiceRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<Response> GetMine(CancellationToken ct) => await _repo.GetMine(ct);
}
