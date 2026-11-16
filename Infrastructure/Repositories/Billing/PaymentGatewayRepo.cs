using System.Net;
using AutoMapper;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.Entities.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Common;
using Core.Models.Billing;
using Infrastructure.Data;
using Infrastructure.Services.Billing.Paymob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Billing;

/// <summary>The clinic's own Paymob keys (ADR-013). Secrets are encrypted before saving and never returned.</summary>
public class PaymentGatewayRepo : IPaymentGatewayRepo
{
    private readonly BillingDbContext _context;
    private readonly PaymentGatewaySecrets _secrets;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PaymentGatewayRepo(BillingDbContext context, PaymentGatewaySecrets secrets, IMapper mapper, ICurrentUser currentUser, IAuditService audit,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _secrets = secrets;
        _mapper = mapper;
        _currentUser = currentUser;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> Get(CancellationToken cancellationToken)
    {
        var settings = await _context.PaymentGatewaySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: settings is null ? null : _mapper.Map<PaymentGatewayDto>(settings));
    }

    public async Task<Response> Save(SavePaymentGatewayModel model, CancellationToken cancellationToken)
    {
        var settings = await _context.PaymentGatewaySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new PaymentGatewaySettings();
            _context.PaymentGatewaySettings.Add(settings);
        }

        settings.Provider = "Paymob";
        settings.PublicKey = model.PublicKey.Trim();
        settings.SecretKeyProtected = _secrets.Protect(model.SecretKey.Trim());
        settings.HmacSecretProtected = _secrets.Protect(model.HmacSecret.Trim());
        settings.IntegrationIds = PaymentGatewaySecrets.FormatIds(model.IntegrationIds);
        settings.UpdatedByUserId = _currentUser.GetUserId() ?? throw new InvalidOperationException("No signed-in user.");

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Billing.GatewaySettingsSaved", nameof(PaymentGatewaySettings), settings.Id,
            detail: $"Paymob integrations {settings.IntegrationIds}", cancellationToken: CancellationToken.None);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK, data: _mapper.Map<PaymentGatewayDto>(settings));
    }
}
