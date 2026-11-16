using Core.Common;
using Core.DTO.Tenancy;
using Core.Entities.Tenancy;
using Core.Enums.Tenancy;
using Core.Interfaces.Tenancy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Repositories.Tenancy;

/// <summary>The public face of the Tenancy module (Tenancy.Contracts). Tenant status is cached for 30 seconds.</summary>
public sealed class TenancyApi : ITenancyApi
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    private readonly TenancyDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _time;

    public TenancyApi(TenancyDbContext context, IMemoryCache cache, TimeProvider time)
    {
        _context = context;
        _cache = cache;
        _time = time;
    }

    public static string InfoCacheKey(long tenantId) => $"t:{tenantId}:info";

    public async Task<TenantInfo?> GetTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(InfoCacheKey(tenantId), out TenantInfo? cached))
            return cached;

        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
            return null;

        var info = await ToInfoAsync(tenant, cancellationToken);
        _cache.Set(InfoCacheKey(tenantId), info, Ttl);
        return info;
    }

    public async Task<TenantInfo?> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
        return tenant is null ? null : await ToInfoAsync(tenant, cancellationToken);
    }

    public static string FeaturesCacheKey(long tenantId) => $"t:{tenantId}:features";

    public async Task<IReadOnlyList<TenantFeatureDto>> GetFeaturesAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(FeaturesCacheKey(tenantId), out IReadOnlyList<TenantFeatureDto>? cached) && cached is not null)
            return cached;

        var planId = await CurrentPlanIdAsync(tenantId, cancellationToken);
        var codes = FeatureCodes.Defaults.Keys.ToList();
        var planValues = planId is null
            ? []
            : await _context.PlanEntitlements.AsNoTracking().Where(e => e.PlanId == planId && codes.Contains(e.Code))
                .ToDictionaryAsync(e => e.Code, e => e.Enabled, cancellationToken);
        var overrides = await _context.TenantFeatureOverrides.AsNoTracking().Where(o => o.TenantId == tenantId)
            .ToDictionaryAsync(o => o.Code, o => o.Enabled, cancellationToken);

        IReadOnlyList<TenantFeatureDto> features = FeatureCodes.Defaults.Select(d =>
            overrides.TryGetValue(d.Key, out var o) ? new TenantFeatureDto { Code = d.Key, Enabled = o, Source = "Override" }
            : planValues.TryGetValue(d.Key, out var p) ? new TenantFeatureDto { Code = d.Key, Enabled = p, Source = "Plan" }
            : new TenantFeatureDto { Code = d.Key, Enabled = d.Value, Source = "Default" }).ToList();

        _cache.Set(FeaturesCacheKey(tenantId), features, Ttl);
        return features;
    }

    public async Task<bool> IsFeatureEnabledAsync(long tenantId, string code, CancellationToken cancellationToken = default) =>
        (await GetFeaturesAsync(tenantId, cancellationToken)).FirstOrDefault(f => f.Code == code)?.Enabled ?? false;

    private Task<long?> CurrentPlanIdAsync(long tenantId, CancellationToken cancellationToken) =>
        _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId &&
                        (s.Status == SubscriptionStatusEnum.Trial || s.Status == SubscriptionStatusEnum.Active || s.Status == SubscriptionStatusEnum.PastDue))
            .OrderByDescending(s => s.StartsAt)
            .Select(s => (long?)s.PlanId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<EntitlementValue> GetEntitlementAsync(long tenantId, string code, CancellationToken cancellationToken = default)
    {
        var planId = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId &&
                        (s.Status == SubscriptionStatusEnum.Trial || s.Status == SubscriptionStatusEnum.Active || s.Status == SubscriptionStatusEnum.PastDue))
            .OrderByDescending(s => s.StartsAt)
            .Select(s => (long?)s.PlanId)
            .FirstOrDefaultAsync(cancellationToken);

        var entitlement = planId is null
            ? null
            : await _context.PlanEntitlements.AsNoTracking().FirstOrDefaultAsync(e => e.PlanId == planId && e.Code == code, cancellationToken);

        if (entitlement is null)
            return new EntitlementValue(!code.StartsWith(EntitlementCodes.ModulePrefix, StringComparison.Ordinal), null);

        return new EntitlementValue(entitlement.Enabled, entitlement.LimitValue);
    }

    /// <summary>The latest subscription; on a plan change the cancelled and the new one start at the same moment, so the id breaks the tie.</summary>
    public static IQueryable<TenantSubscription> LatestSubscription(TenancyDbContext context, long tenantId) =>
        context.TenantSubscriptions.Where(s => s.TenantId == tenantId).OrderByDescending(s => s.StartsAt).ThenByDescending(s => s.Id);

    private async Task<TenantInfo> ToInfoAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var latest = await LatestSubscription(_context, tenant.Id).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var (readOnly, blocked) = SubscriptionAccess.Evaluate(latest, _time.GetUtcNow());
        return new TenantInfo(tenant.Id, tenant.Slug, tenant.Name, tenant.Status.ToString(), tenant.IsUsable && !blocked, tenant.CurrencyCode,
            tenant.IsUsable && readOnly);
    }
}
