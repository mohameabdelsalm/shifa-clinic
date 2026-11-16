using System.Net;
using Core;
using Core.Common;
using Core.Entities.Billing;
using Core.Interfaces.Billing;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Services.Billing;

/// <summary>Used until an online payment gateway is configured: online refunds are refused with a clear message.</summary>
public sealed class UnavailableOnlineRefunds : IOnlineRefunds
{
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public UnavailableOnlineRefunds(IStringLocalizer<LanguageResource> localizer)
    {
        _localizer = localizer;
    }

    public Task<string> RefundAsync(Payment payment, decimal amount, CancellationToken cancellationToken) =>
        throw new BusinessException(_localizer["OnlinePaymentsNotConfigured"].Value, HttpStatusCode.BadRequest);
}
