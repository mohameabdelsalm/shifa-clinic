using Core.Common;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Core.Models.Clinic;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Clinic;

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
