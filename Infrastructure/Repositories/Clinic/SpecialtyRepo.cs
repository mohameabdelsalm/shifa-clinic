using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Clinic;
using Core.Entities.Clinic;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Models.Clinic;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Clinic;

/// <summary>Medical specialties of the tenant. They aren't branch-owned, so no branch scope applies.</summary>
public class SpecialtyRepo : ISpecialtyRepo
{
    private readonly ClinicDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public SpecialtyRepo(ClinicDbContext context, IMapper mapper, IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> GetAll(CancellationToken cancellationToken)
    {
        var specialties = await _context.Specialties.AsNoTracking().OrderBy(s => s.NameEn)
            .ProjectTo<SpecialtyDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: specialties);
    }

    public async Task<Response> Add(AddSpecialtyModel model, CancellationToken cancellationToken)
    {
        var nameEn = model.NameEn.Trim();
        if (await _context.Specialties.AnyAsync(s => s.NameEn == nameEn, cancellationToken))
            throw new BusinessException(_localizer["DuplicateName"].Value, HttpStatusCode.BadRequest);

        var specialty = _mapper.Map<Specialty>(model);
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

        _mapper.Map(model, specialty);
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
