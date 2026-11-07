using System.Net;
using Core;
using Core.Common;
using Core.DTO.Patients;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Patients;

/// <summary>
/// What a patient or guardian sees after signing in with a phone OTP. The phone number comes from the validated token
/// (IAccessContext), never from the request. A patient the phone isn't linked to is "not found".
/// </summary>
public class PortalPatientRepo : IPortalPatientRepo
{
    private readonly PatientsDbContext _context;
    private readonly IAccessContext _access;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PortalPatientRepo(PatientsDbContext context, IAccessContext access, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _access = access;
        _localizer = localizer;
    }

    public async Task<Response> GetMine(CancellationToken cancellationToken)
    {
        var patients = await Linked().OrderBy(p => p.FullName).ToListAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: patients);
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var patient = await Linked().FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["PatientNotFound"].Value, HttpStatusCode.NotFound);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: patient);
    }

    private IQueryable<PortalPatientDto> Linked()
    {
        var phone = _access.PortalPhoneNumber ?? throw new InvalidOperationException("Not a portal request.");

        return
            from a in _context.PatientAccountAccesses.AsNoTracking()
            where a.PhoneNumber == phone
            join p in _context.Patients.AsNoTracking() on a.PatientId equals p.Id
            select new PortalPatientDto
            {
                Id = p.Id,
                Mrn = p.Mrn,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender.ToString(),
                Relationship = a.Relationship.ToString(),
                CanBook = a.CanBook,
            };
    }
}
