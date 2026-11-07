using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.Core.Interfaces.Common;
using Shifa.Tenancy.Data;
using Shifa.Tenancy.DTO;
using Shifa.Tenancy.Entities;
using Shifa.Tenancy.Interfaces;
using Shifa.Tenancy.Models;

namespace Shifa.Tenancy.Repositories;

public class PlanRepo : IPlanRepo
{
    private readonly TenancyDbContext _context;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PlanRepo(TenancyDbContext context, IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> GetAll(CancellationToken cancellationToken)
    {
        var plans = await ProjectPlans(_context.Plans.AsNoTracking()).OrderBy(p => p.Price).ToListAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: plans);
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var plan = await ProjectPlans(_context.Plans.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: plan);
    }

    public async Task<Response> Add(AddPlanModel model, CancellationToken cancellationToken)
    {
        var code = model.Code.Trim().ToUpperInvariant();
        if (await _context.Plans.AnyAsync(p => p.Code == code, cancellationToken))
            throw new BusinessException(_localizer["PlanCodeDuplicated"].Value, HttpStatusCode.BadRequest);

        var plan = new Plan
        {
            Code = code,
            Name = model.Name.Trim(),
            Price = model.Price,
            BillingPeriod = model.BillingPeriod,
            TrialDays = model.TrialDays,
        };
        _context.Plans.Add(plan);
        foreach (var entitlement in model.Entitlements.DistinctBy(e => e.Code))
        {
            _context.PlanEntitlements.Add(new PlanEntitlement
            {
                PlanId = plan.Id,
                Code = entitlement.Code,
                Enabled = entitlement.Enabled,
                LimitValue = entitlement.LimitValue,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Platform.PlanCreated", "Plan", plan.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: plan.Id);
    }

    public async Task<Response> Update(UpdatePlanModel model, CancellationToken cancellationToken)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken) ?? throw NotFound();
        plan.Name = model.Name.Trim();
        plan.Price = model.Price;
        plan.TrialDays = model.TrialDays;
        plan.IsActive = model.IsActive;

        var wanted = model.Entitlements.DistinctBy(e => e.Code).ToList();
        var current = await _context.PlanEntitlements.Where(e => e.PlanId == plan.Id).ToListAsync(cancellationToken);

        _context.PlanEntitlements.RemoveRange(current.Where(e => wanted.All(w => w.Code != e.Code)));
        foreach (var entitlement in wanted)
        {
            var existing = current.FirstOrDefault(e => e.Code == entitlement.Code);
            if (existing is null)
            {
                _context.PlanEntitlements.Add(new PlanEntitlement
                {
                    PlanId = plan.Id,
                    Code = entitlement.Code,
                    Enabled = entitlement.Enabled,
                    LimitValue = entitlement.LimitValue,
                });
            }
            else
            {
                existing.Enabled = entitlement.Enabled;
                existing.LimitValue = entitlement.LimitValue;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Platform.PlanUpdated", "Plan", plan.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    private IQueryable<PlanDto> ProjectPlans(IQueryable<Plan> plans) =>
        plans.Select(p => new PlanDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Price = p.Price,
            BillingPeriod = p.BillingPeriod.ToString(),
            TrialDays = p.TrialDays,
            IsActive = p.IsActive,
            Entitlements = _context.PlanEntitlements.Where(e => e.PlanId == p.Id)
                .OrderBy(e => e.Code)
                .Select(e => new PlanEntitlementDto { Code = e.Code, Enabled = e.Enabled, LimitValue = e.LimitValue })
                .ToList(),
        });

    private BusinessException NotFound() => new(_localizer["PlanNotFound"].Value, HttpStatusCode.NotFound);
}
