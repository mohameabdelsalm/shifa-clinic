using Core.Common;

namespace Core.Interfaces.Patients;

public interface IPortalPatientRepo
{
    Task<Response> GetMine(CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
}
