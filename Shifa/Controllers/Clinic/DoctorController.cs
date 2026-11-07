using Core.Common;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Core.Models.Clinic;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Clinic;

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

    /// <summary>Links the doctor to a staff account (or removes the link), so that account can open and write visits.</summary>
    [HttpPut("account")]
    [Permission(Permissions.DoctorsManage)]
    public async Task<Response> LinkAccount(LinkDoctorAccountModel model, CancellationToken ct) => await _repo.LinkAccount(model, ct);

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
