using System.Net;
using Core;
using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Common;
using Core.Interfaces.Tenancy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Tenancy;

/// <summary>The clinic's own subscription (api/subscription), including the read-only state of A6.</summary>
public class SubscriptionRepo : ISubscriptionRepo
{
    private readonly TenancyDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public SubscriptionRepo(TenancyDbContext context, ITenantContext tenant, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _tenant = tenant;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> GetMine(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new BusinessException(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);
        var latest = await TenancyApi.LatestSubscription(_context, tenantId).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var plan = latest is null ? null : await _context.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == latest.PlanId, cancellationToken);

        var endedAt = SubscriptionAccess.EndedAt(latest);
        var (readOnly, _) = SubscriptionAccess.Evaluate(latest, _time.GetUtcNow());
        var dto = new MySubscriptionDto
        {
            PlanCode = plan?.Code,
            PlanName = plan?.Name,
            Status = latest?.Status.ToString(),
            TrialEndsAt = latest?.TrialEndsAt,
            EndsAt = latest?.EndsAt,
            EndedAt = endedAt <= _time.GetUtcNow() ? endedAt : null,
            IsReadOnly = readOnly,
            BlockedFrom = endedAt + SubscriptionAccess.ReadOnlyGrace,
        };
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: dto);
    }
}
