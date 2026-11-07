using System.Net;
using AutoMapper;
using Core;
using Core.Common;
using Core.DTO.Clinical;
using Core.Enums.Clinical;
using Core.Interfaces.Clinical;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Clinical;

/// <summary>Prescriptions of completed visits, for the patients linked to the signed-in phone (ADR-010).</summary>
public class PortalPrescriptionRepo : IPortalPrescriptionRepo
{
    private const int MaxPrescriptions = 100;

    private readonly ClinicalDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPatientsApi _patients;
    private readonly IAccessContext _access;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PortalPrescriptionRepo(ClinicalDbContext context, IMapper mapper, IPatientsApi patients, IAccessContext access,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _patients = patients;
        _access = access;
        _localizer = localizer;
    }

    public async Task<Response> GetMine(CancellationToken cancellationToken)
    {
        var phone = _access.PortalPhoneNumber ?? throw new InvalidOperationException("Not a portal request.");
        var patientIds = (await _patients.GetPortalPatientsAsync(phone, cancellationToken)).Select(p => p.PatientId).ToList();

        var rows = await _context.Visits.AsNoTracking()
            .Where(v => patientIds.Contains(v.PatientId) && v.Status == VisitStatusEnum.Completed)
            .Join(_context.Prescriptions.AsNoTracking(), v => v.Id, p => p.VisitId, (v, p) => new { Visit = v, Prescription = p })
            .OrderByDescending(x => x.Visit.CompletedAt)
            .Take(MaxPrescriptions)
            .ToListAsync(cancellationToken);

        var prescriptionIds = rows.Select(r => r.Prescription.Id).ToList();
        var items = await _context.PrescriptionItems.AsNoTracking()
            .Where(i => prescriptionIds.Contains(i.PrescriptionId))
            .OrderBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var result = rows.Select(r =>
        {
            var dto = _mapper.Map<PortalPrescriptionDto>(r.Visit);
            dto.Notes = r.Prescription.Notes;
            dto.Items = _mapper.Map<List<PrescriptionItemDto>>(items.Where(i => i.PrescriptionId == r.Prescription.Id));
            return dto;
        }).ToList();

        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: result);
    }
}
