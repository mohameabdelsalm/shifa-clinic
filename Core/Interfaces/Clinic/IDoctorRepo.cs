using Core.Common;
using Core.Enums.Common;
using Core.Models.Clinic;

namespace Core.Interfaces.Clinic;

public interface IDoctorRepo
{
    Task<Response> Search(SearchDoctorsModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddDoctorModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateDoctorModel model, CancellationToken cancellationToken);
    Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken);
    Task<Response> SetSchedule(SetDoctorScheduleModel model, CancellationToken cancellationToken);
    Task<Response> AddTimeOff(AddTimeOffModel model, CancellationToken cancellationToken);
    Task<Response> DeleteTimeOff(long id, CancellationToken cancellationToken);
}
