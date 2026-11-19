using Core.Common;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Core.Models.Clinic;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Clinic;

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
