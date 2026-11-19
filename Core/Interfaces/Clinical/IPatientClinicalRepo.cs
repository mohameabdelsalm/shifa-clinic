using Core.Common;
using Core.Models.Clinical;

namespace Core.Interfaces.Clinical;

public interface IPatientClinicalRepo
{
    Task<Response> Summary(long patientId, CancellationToken cancellationToken);
    Task<Response> AddAllergy(AddAllergyModel model, CancellationToken cancellationToken);
    Task<Response> DeleteAllergy(long id, CancellationToken cancellationToken);
    Task<Response> AddCondition(AddConditionModel model, CancellationToken cancellationToken);
    Task<Response> DeleteCondition(long id, CancellationToken cancellationToken);
}
