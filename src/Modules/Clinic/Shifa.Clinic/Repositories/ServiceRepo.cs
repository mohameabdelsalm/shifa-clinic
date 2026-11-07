using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shifa.Clinic.Data;
using Shifa.Clinic.DTO;
using Shifa.Clinic.Entities;
using Shifa.Clinic.Interfaces;
using Shifa.Clinic.Models;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.Core.Enums.Common;
using Shifa.Core.Interfaces.Common;
using Shifa.Core.Pagination;
using Shifa.Infrastructure.Data;

namespace Shifa.Clinic.Repositories;

/// <summary>
/// Services and their per-branch prices. Services are shared by the tenant; prices belong to a branch,
/// so a member limited to some branches sees and sets prices only for those.
/// A price change keeps history (the old row is closed, a new one opened) so invoices can show what was charged.
/// </summary>
public class ServiceRepo : IServiceRepo
{
    private readonly ClinicDbContext _context;
    private readonly IAccessContext _access;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public ServiceRepo(ClinicDbContext context, IAccessContext access, IAuditService audit, TimeProvider time,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _access = access;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Search(SearchServicesModel model, CancellationToken cancellationToken)
    {
        var query = _context.Services.AsNoTracking();
        if (model.SpecialtyId is not null)
            query = query.Where(s => s.SpecialtyId == model.SpecialtyId);
        if (!string.IsNullOrWhiteSpace(model.Text))
        {
            var text = model.Text.Trim();
            query = query.Where(s => s.NameEn.Contains(text) || s.NameAr.Contains(text));
        }

        var page = await query.OrderBy(s => s.NameEn).ToPagedListAsync(model, cancellationToken);
        var dtos = await ToDtosAsync(page.Items, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK,
            data: new PagedList<ServiceDto>(dtos, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken) ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: (await ToDtosAsync([service], cancellationToken))[0]);
    }

    public async Task<Response> Add(AddServiceModel model, CancellationToken cancellationToken)
    {
        if (!await _context.Specialties.AnyAsync(s => s.Id == model.SpecialtyId && s.Status == StatusEnum.Active, cancellationToken))
            throw new BusinessException(_localizer["SpecialtyNotFound"].Value, HttpStatusCode.BadRequest);

        var nameEn = model.NameEn.Trim();
        if (await _context.Services.AnyAsync(s => s.SpecialtyId == model.SpecialtyId && s.NameEn == nameEn, cancellationToken))
            throw new BusinessException(_localizer["DuplicateName"].Value, HttpStatusCode.BadRequest);

        var service = new Service
        {
            SpecialtyId = model.SpecialtyId,
            NameEn = nameEn,
            NameAr = model.NameAr.Trim(),
            DurationMinutes = model.DurationMinutes,
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Services.Created", "Service", service.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: service.Id);
    }

    public async Task<Response> Update(UpdateServiceModel model, CancellationToken cancellationToken)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == model.Id, cancellationToken) ?? throw NotFound();
        var nameEn = model.NameEn.Trim();
        if (await _context.Services.AnyAsync(s => s.Id != service.Id && s.SpecialtyId == service.SpecialtyId && s.NameEn == nameEn, cancellationToken))
            throw new BusinessException(_localizer["DuplicateName"].Value, HttpStatusCode.BadRequest);

        service.NameEn = nameEn;
        service.NameAr = model.NameAr.Trim();
        service.DurationMinutes = model.DurationMinutes;
        await _context.SaveChangesAsync(cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken) ?? throw NotFound();
        service.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync($"Services.{status}", "Service", id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> SetPrice(SetServicePriceModel model, CancellationToken cancellationToken)
    {
        if (!await _context.Services.AnyAsync(s => s.Id == model.ServiceId, cancellationToken))
            throw NotFound();

        if (!await BranchInScopeAsync(model.BranchId, cancellationToken))
            throw new BusinessException(_localizer["BranchNotFound"].Value, HttpStatusCode.NotFound);

        var current = await _context.ServicePrices
            .FirstOrDefaultAsync(p => p.ServiceId == model.ServiceId && p.BranchId == model.BranchId && p.EffectiveTo == null, cancellationToken);
        if (current is not null && current.Price == model.Price)
            return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);

        var now = _time.GetUtcNow();

        // Two saves in one transaction: the old row must be closed before the new open row is inserted,
        // because only one open price per service and branch is allowed (unique filtered index).
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (current is not null)
        {
            current.EffectiveTo = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        _context.ServicePrices.Add(new ServicePrice
        {
            ServiceId = model.ServiceId,
            BranchId = model.BranchId,
            Price = model.Price,
            EffectiveFrom = now,
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _audit.LogAsync("Services.PriceChanged", "Service", model.ServiceId, detail: $"branch {model.BranchId}", cancellationToken: cancellationToken);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    private async Task<bool> BranchInScopeAsync(long branchId, CancellationToken cancellationToken)
    {
        var scope = _access.BranchIds.ToList();
        return await _context.Branches.AnyAsync(
            b => b.Id == branchId && b.Status == StatusEnum.Active && (_access.HasAllBranches || scope.Contains(b.Id)), cancellationToken);
    }

    /// <summary>Attaches the current prices of the caller's branches to the services.</summary>
    private async Task<List<ServiceDto>> ToDtosAsync(List<Service> services, CancellationToken cancellationToken)
    {
        var ids = services.Select(s => s.Id).ToList();
        var allBranches = _access.HasAllBranches;
        var scope = _access.BranchIds.ToList();

        var prices = await _context.ServicePrices.AsNoTracking()
            .Where(p => ids.Contains(p.ServiceId) && p.EffectiveTo == null && (allBranches || scope.Contains(p.BranchId)))
            .ToListAsync(cancellationToken);

        return services.Select(s => new ServiceDto
        {
            Id = s.Id,
            SpecialtyId = s.SpecialtyId,
            NameEn = s.NameEn,
            NameAr = s.NameAr,
            DurationMinutes = s.DurationMinutes,
            Status = s.Status.ToString(),
            Prices = prices.Where(p => p.ServiceId == s.Id)
                .Select(p => new ServicePriceDto { BranchId = p.BranchId, Price = p.Price, EffectiveFrom = p.EffectiveFrom })
                .OrderBy(p => p.BranchId).ToList(),
        }).ToList();
    }

    private BusinessException NotFound() => new(_localizer["ServiceNotFound"].Value, HttpStatusCode.NotFound);
}
