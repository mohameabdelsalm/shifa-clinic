using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using Core;
using Core.Common;
using Core.DTO.Identity;
using Core.DTO.Tenancy;
using Core.Entities.Tenancy;
using Core.Enums.Tenancy;
using Core.Interfaces.Common;
using Core.Interfaces.Identity;
using Core.Interfaces.Tenancy;
using Core.Models.Tenancy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Tenancy;

/// <summary>Platform Admin operations on tenants (api/platform/tenants). Every state change is audited.</summary>
public class TenantRepo : ITenantRepo
{
    private readonly TenancyDbContext _context;
    private readonly IMapper _mapper;
    private readonly IIdentityApi _identity;
    private readonly IAuditService _audit;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public TenantRepo(TenancyDbContext context, IMapper mapper, IIdentityApi identity, IAuditService audit, IMemoryCache cache,
        TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _identity = identity;
        _audit = audit;
        _cache = cache;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Search(SearchTenantsModel model, CancellationToken cancellationToken)
    {
        var tenants = _context.Tenants.AsNoTracking().Where(FilterTenants(model));
        var paged = await SortTenants(ProjectTenants(tenants), model).ToPagedListAsync(model, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    private static Expression<Func<Tenant, bool>> FilterTenants(SearchTenantsModel model)
    {
        var text = model.SearchText;
        return t => (text == null || t.Name.Contains(text) || t.Slug.Contains(text))
                    && (model.Status == null || t.Status == model.Status);
    }

    /// <summary>"date" uses the tenant id (HiLo, creation order).</summary>
    private static IQueryable<TenantDto> SortTenants(IQueryable<TenantDto> query, SearchTenantsModel model) => model.SortBy switch
    {
        "name" => query.OrderBy(model.SortType, t => t.Name).ThenBy(t => t.Id),
        "slug" => query.OrderBy(model.SortType, t => t.Slug).ThenBy(t => t.Id),
        "status" => query.OrderBy(model.SortType, t => t.Status).ThenBy(t => t.Id),
        _ => query.OrderBy(model.SortType, t => t.Id),
    };

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var tenant = await ProjectTenants(_context.Tenants.AsNoTracking().Where(t => t.Id == id)).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: tenant);
    }

    public async Task<Response> Add(AddTenantModel model, CancellationToken cancellationToken)
    {
        var slug = model.Slug.Trim().ToLowerInvariant();
        if (await _context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
            throw new BusinessException(_localizer["TenantSlugDuplicated"].Value, HttpStatusCode.BadRequest);

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == model.PlanId && p.IsActive, cancellationToken)
            ?? throw new BusinessException(_localizer["PlanNotFound"].Value, HttpStatusCode.NotFound);

        var now = _time.GetUtcNow();
        var tenant = _mapper.Map<Tenant>(model);
        _context.Tenants.Add(tenant);

        var subscription = new TenantSubscription
        {
            TenantId = tenant.Id,
            PlanId = plan.Id,
            Status = SubscriptionStatusEnum.Trial,
            StartsAt = now,
            TrialEndsAt = now.AddDays(plan.TrialDays),
        };
        _context.TenantSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Default roles + first TenantAdmin, in the new tenant's own scope. Another module's tables, so no shared transaction:
            // if it fails, the tenant is removed again (compensation).
            await _identity.ProvisionTenantAsync(
                new ProvisionTenantRequest(tenant.Id, model.AdminFullName, model.AdminEmail, model.AdminPassword), cancellationToken);
        }
        catch
        {
            _context.TenantSubscriptions.Remove(subscription);
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        await _audit.LogAsync("Platform.TenantCreated", "Tenant", tenant.Id, targetTenantId: tenant.Id, cancellationToken: cancellationToken);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: tenant.Id);
    }

    public Task<Response> Suspend(long id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, tenant => tenant.TrySuspend(), "Platform.TenantSuspended", cancellationToken);

    public Task<Response> Activate(long id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, tenant => tenant.TryActivate(), "Platform.TenantActivated", cancellationToken);

    public async Task<Response> ChangePlan(ChangePlanModel model, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == model.TenantId, cancellationToken) ?? throw NotFound();
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == model.PlanId && p.IsActive, cancellationToken)
            ?? throw new BusinessException(_localizer["PlanNotFound"].Value, HttpStatusCode.NotFound);

        var now = _time.GetUtcNow();
        var current = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenant.Id && s.Status != SubscriptionStatusEnum.Cancelled && s.Status != SubscriptionStatusEnum.Expired)
            .ToListAsync(cancellationToken);
        foreach (var subscription in current)
        {
            subscription.Status = SubscriptionStatusEnum.Cancelled;
            subscription.EndsAt = now;
        }

        _context.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantId = tenant.Id,
            PlanId = plan.Id,
            Status = SubscriptionStatusEnum.Active,
            StartsAt = now,
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Platform.TenantPlanChanged", "Tenant", tenant.Id, targetTenantId: tenant.Id, detail: $"plan {plan.Code}", cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> RecordPayment(RecordPaymentModel model, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == model.TenantId, cancellationToken) ?? throw NotFound();
        var subscription = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenant.Id)
            .OrderByDescending(s => s.StartsAt)
            .FirstOrDefaultAsync(cancellationToken) ?? throw NotFound();

        var now = _time.GetUtcNow();
        _context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            TenantSubscriptionId = subscription.Id,
            Amount = model.Amount,
            CurrencyCode = tenant.CurrencyCode,
            PaidAt = now,
            Reference = model.Reference?.Trim(),
        });
        if (subscription.Status is SubscriptionStatusEnum.Trial or SubscriptionStatusEnum.PastDue)
            subscription.Status = SubscriptionStatusEnum.Active;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Platform.PaymentRecorded", "Tenant", tenant.Id, targetTenantId: tenant.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK);
    }

    private async Task<Response> ChangeStatusAsync(long id, Func<Tenant, bool> change, string auditAction, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken) ?? throw NotFound();
        if (!change(tenant))
            throw new BusinessException(_localizer["TenantInactive"].Value, HttpStatusCode.BadRequest);

        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(TenancyApi.InfoCacheKey(id));
        await _audit.LogAsync(auditAction, "Tenant", id, targetTenantId: id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    private IQueryable<TenantDto> ProjectTenants(IQueryable<Tenant> tenants) =>
        tenants.Select(t => new
        {
            Tenant = t,
            Subscription = _context.TenantSubscriptions.Where(s => s.TenantId == t.Id).OrderByDescending(s => s.StartsAt).FirstOrDefault(),
        })
        .Select(x => new TenantDto
        {
            Id = x.Tenant.Id,
            Name = x.Tenant.Name,
            Slug = x.Tenant.Slug,
            Status = x.Tenant.Status.ToString(),
            DefaultTimeZoneId = x.Tenant.DefaultTimeZoneId,
            CurrencyCode = x.Tenant.CurrencyCode,
            PlanCode = x.Subscription == null ? null : _context.Plans.Where(p => p.Id == x.Subscription.PlanId).Select(p => p.Code).FirstOrDefault(),
            SubscriptionStatus = x.Subscription == null ? null : x.Subscription.Status.ToString(),
            TrialEndsAt = x.Subscription == null ? null : x.Subscription.TrialEndsAt,
            SubscriptionEndsAt = x.Subscription == null ? null : x.Subscription.EndsAt,
        });

    private BusinessException NotFound() => new(_localizer["TenantNotFound"].Value, HttpStatusCode.NotFound);
}
