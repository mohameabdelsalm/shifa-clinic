using Shifa.Core.Common;
using Shifa.Core.Enums.Common;
using Shifa.Clinic.Models;

namespace Shifa.Clinic.Interfaces;

public interface IBranchRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> Search(SearchBranchesModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddBranchModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateBranchModel model, CancellationToken cancellationToken);
    Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken);
}

public interface ISpecialtyRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> Add(AddSpecialtyModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateSpecialtyModel model, CancellationToken cancellationToken);
    Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken);
}

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

public interface IServiceRepo
{
    Task<Response> Search(SearchServicesModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddServiceModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateServiceModel model, CancellationToken cancellationToken);
    Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken);
    Task<Response> SetPrice(SetServicePriceModel model, CancellationToken cancellationToken);
}
