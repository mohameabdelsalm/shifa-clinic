using System.Net;
using Core;
using Core.Common;
using Core.Interfaces.Common;
using Core.Interfaces.Tenancy;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Tenancy;

public class FeatureRepo : IFeatureRepo
{
    private readonly ITenancyApi _tenancy;
    private readonly ITenantContext _tenant;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public FeatureRepo(ITenancyApi tenancy, ITenantContext tenant, IStringLocalizer<LanguageResource> localizer)
    {
        _tenancy = tenancy;
        _tenant = tenant;
        _localizer = localizer;
    }

    public async Task<Response> GetMine(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new BusinessException(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: await _tenancy.GetFeaturesAsync(tenantId, cancellationToken));
    }
}
