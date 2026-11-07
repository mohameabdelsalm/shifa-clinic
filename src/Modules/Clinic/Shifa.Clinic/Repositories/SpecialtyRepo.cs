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

namespace Shifa.Clinic.Repositories;

/// <summary>Medical specialties of the tenant. They aren't branch-owned, so no branch scope applies.</summary>
public class SpecialtyRepo : ISpecialtyRepo
{
    private readonly ClinicDbContext _context;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public SpecialtyRepo(ClinicDbContext context, IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> GetAll(CancellationToken cancellationToken)
    {
        var specialties = await _context.Specialties.AsNoTracking().OrderBy(s => s.NameEn)
            .Select(s => new SpecialtyDto { Id = s.Id, NameEn = s.NameEn, NameAr = s.NameAr, Status = s.Status.ToString() })
            .ToListAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: specialties);
    }

    public async Task<Response> Add(AddSpecialtyModel model, CancellationToken cancellationToken)
    {
        var nameEn = model.NameEn.Trim();
        if (await _context.Specialties.AnyAsync(s => s.NameEn == nameEn, cancellationToken))
            throw new BusinessException(_localizer["DuplicateName"].Value, HttpStatusCode.BadRequest);

        var specialty = new Specialty { NameEn = nameEn, NameAr = model.NameAr.Trim() };
        _context.Specialties.Add(specialty);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Specialties.Created", "Specialty", specialty.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: specialty.Id);
    }

    public async Task<Response> Update(UpdateSpecialtyModel model, CancellationToken cancellationToken)
    {
        var specialty = await _context.Specialties.FirstOrDefaultAsync(s => s.Id == model.Id, cancellationToken) ?? throw NotFound();
        var nameEn = model.NameEn.Trim();
        if (await _context.Specialties.AnyAsync(s => s.Id != specialty.Id && s.NameEn == nameEn, cancellationToken))
            throw new BusinessException(_localizer["DuplicateName"].Value, HttpStatusCode.BadRequest);

        specialty.NameEn = nameEn;
        specialty.NameAr = model.NameAr.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken)
    {
        var specialty = await _context.Specialties.FirstOrDefaultAsync(s => s.Id == id, cancellationToken) ?? throw NotFound();
        specialty.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync($"Specialties.{status}", "Specialty", id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    private BusinessException NotFound() => new(_localizer["SpecialtyNotFound"].Value, HttpStatusCode.NotFound);
}
