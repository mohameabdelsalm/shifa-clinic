using Core.Common;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Core.Models.Clinic;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Clinic;

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
