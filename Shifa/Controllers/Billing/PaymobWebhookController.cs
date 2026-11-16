using System.Net;
using System.Text;
using Core;
using Core.Common;
using Core.Interfaces.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Shifa.Controllers.Billing;

/// <summary>
/// Paymob transaction callbacks (ADR-013). Anonymous by design: trust comes only from the HMAC, checked with the
/// clinic's own secret. 401 when the signature is wrong, 200 otherwise (so Paymob stops retrying).
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("webhooks")]
[Route("api/webhooks/paymob")]
public class PaymobWebhookController : ControllerBase
{
    private const int MaxBodyBytes = 64 * 1024;

    private readonly IPaymobWebhookHandler _handler;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PaymobWebhookController(IPaymobWebhookHandler handler, IStringLocalizer<LanguageResource> localizer)
    {
        _handler = handler;
        _localizer = localizer;
    }

    [HttpPost("{tenantId:long}")]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> Transaction(long tenantId, [FromQuery] string? hmac, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);

        var outcome = await _handler.HandleAsync(tenantId, hmac, body, ct);
        return outcome == WebhookOutcome.Accepted
            ? Ok(new Response(_localizer["Received"].Value, HttpStatusCode.OK))
            : Unauthorized(new Response(_localizer["InvalidSignature"].Value, HttpStatusCode.Unauthorized, isSuccess: false));
    }
}
