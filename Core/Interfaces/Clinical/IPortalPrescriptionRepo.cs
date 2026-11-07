using Core.Common;

namespace Core.Interfaces.Clinical;

public interface IPortalPrescriptionRepo
{
    Task<Response> GetMine(CancellationToken cancellationToken);
}
