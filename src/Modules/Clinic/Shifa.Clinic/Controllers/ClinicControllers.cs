using Microsoft.AspNetCore.Mvc;
using Shifa.Clinic.Interfaces;
using Shifa.Clinic.Models;
using Shifa.Core.Common;
using Shifa.Core.Enums.Common;
using Shifa.Infrastructure.Auth;

namespace Shifa.Clinic.Controllers;

[ApiController]
[Route("api/branches")]
public class BranchController : ControllerBase
{
    private readonly IBranchRepo _repo;

    public BranchController(IBranchRepo repo)
    {
        _repo = repo;
    }

    [HttpGet("all")]
    [Permission(Permissions.BranchesRead)]
    public async Task<Response> GetAll(CancellationToken ct) => await _repo.GetAll(ct);

    [HttpPost("search")]
    [Permission(Permissions.BranchesRead)]
    public async Task<Response> Search(SearchBranchesModel model, CancellationToken ct) => await _repo.Search(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.BranchesRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    [HttpPost]
    [Permission(Permissions.BranchesManage)]
    public async Task<Response> Add(AddBranchModel model, CancellationToken ct) => await _repo.Add(model, ct);

    [HttpPut]
    [Permission(Permissions.BranchesManage)]
    public async Task<Response> Update(UpdateBranchModel model, CancellationToken ct) => await _repo.Update(model, ct);

    [HttpPut("{id}/status/{status}")]
    [Permission(Permissions.BranchesManage)]
    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken ct) => await _repo.ChangeStatus(id, status, ct);
}

[ApiController]
[Route("api/specialties")]
public class SpecialtiesController : ControllerBase
{
    private readonly ISpecialtyRepo _repo;

    public SpecialtiesController(ISpecialtyRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [Permission(Permissions.ServicesRead)]
    public async Task<Response> GetAll(CancellationToken ct) => await _repo.GetAll(ct);

    [HttpPost]
    [Permission(Permissions.ServicesManage)]
    public async Task<Response> Add(AddSpecialtyModel model, CancellationToken ct) => await _repo.Add(model, ct);

    [HttpPut]
    [Permission(Permissions.ServicesManage)]
    public async Task<Response> Update(UpdateSpecialtyModel model, CancellationToken ct) => await _repo.Update(model, ct);

    [HttpPut("{id}/status/{status}")]
    [Permission(Permissions.ServicesManage)]
    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken ct) => await _repo.ChangeStatus(id, status, ct);
}

[ApiController]
[Route("api/doctors")]
public class DoctorController : ControllerBase
{
    private readonly IDoctorRepo _repo;

    public DoctorController(IDoctorRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("search")]
    [Permission(Permissions.DoctorsRead)]
    public async Task<Response> Search(SearchDoctorsModel model, CancellationToken ct) => await _repo.Search(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.DoctorsRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    [HttpPost]
    [Permission(Permissions.DoctorsManage)]
    public async Task<Response> Add(AddDoctorModel model, CancellationToken ct) => await _repo.Add(model, ct);

    [HttpPut]
    [Permission(Permissions.DoctorsManage)]
    public async Task<Response> Update(UpdateDoctorModel model, CancellationToken ct) => await _repo.Update(model, ct);

    [HttpPut("{id}/status/{status}")]
    [Permission(Permissions.DoctorsManage)]
    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken ct) => await _repo.ChangeStatus(id, status, ct);

    [HttpPut("schedule")]
    [Permission(Permissions.DoctorsManage)]
    public async Task<Response> SetSchedule(SetDoctorScheduleModel model, CancellationToken ct) => await _repo.SetSchedule(model, ct);

    [HttpPost("time-off")]
    [Permission(Permissions.DoctorsManage)]
    public async Task<Response> AddTimeOff(AddTimeOffModel model, CancellationToken ct) => await _repo.AddTimeOff(model, ct);

    [HttpDelete("time-off/{id}")]
    [Permission(Permissions.DoctorsManage)]
    public async Task<Response> DeleteTimeOff(long id, CancellationToken ct) => await _repo.DeleteTimeOff(id, ct);
}

[ApiController]
[Route("api/services")]
public class ServiceController : ControllerBase
{
    private readonly IServiceRepo _repo;

    public ServiceController(IServiceRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("search")]
    [Permission(Permissions.ServicesRead)]
    public async Task<Response> Search(SearchServicesModel model, CancellationToken ct) => await _repo.Search(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.ServicesRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    [HttpPost]
    [Permission(Permissions.ServicesManage)]
    public async Task<Response> Add(AddServiceModel model, CancellationToken ct) => await _repo.Add(model, ct);

    [HttpPut]
    [Permission(Permissions.ServicesManage)]
    public async Task<Response> Update(UpdateServiceModel model, CancellationToken ct) => await _repo.Update(model, ct);

    [HttpPut("{id}/status/{status}")]
    [Permission(Permissions.ServicesManage)]
    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken ct) => await _repo.ChangeStatus(id, status, ct);

    [HttpPut("price")]
    [Permission(Permissions.ServicesManage)]
    public async Task<Response> SetPrice(SetServicePriceModel model, CancellationToken ct) => await _repo.SetPrice(model, ct);
}
