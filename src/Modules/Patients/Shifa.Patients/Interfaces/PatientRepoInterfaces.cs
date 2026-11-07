using Shifa.Core.Common;
using Shifa.Patients.Models;

namespace Shifa.Patients.Interfaces;

public interface IPatientRepo
{
    Task<Response> Search(SearchPatientsModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddPatientModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdatePatientModel model, CancellationToken cancellationToken);
    Task<Response> AddGuardian(AddGuardianModel model, CancellationToken cancellationToken);
    Task<Response> DeleteGuardian(long id, CancellationToken cancellationToken);
    Task<Response> GrantConsent(GrantConsentModel model, CancellationToken cancellationToken);
    Task<Response> RevokeConsent(long id, CancellationToken cancellationToken);
    Task<Response> LinkPortalAccess(LinkPortalAccessModel model, CancellationToken cancellationToken);
    Task<Response> RemovePortalAccess(long id, CancellationToken cancellationToken);
}

public interface IPortalPatientRepo
{
    Task<Response> GetMine(CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
}
