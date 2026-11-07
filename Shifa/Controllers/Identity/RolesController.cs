using Core.Common;
using Core.Interfaces.Identity;
using Core.Models.Identity;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Identity;

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepo _roleRepo;

    public RolesController(IRoleRepo roleRepo)
    {
        _roleRepo = roleRepo;
    }

    [HttpGet]
    [Permission(Permissions.RolesRead)]
    public async Task<Response> GetAll(CancellationToken cancellationToken)
    {
        return await _roleRepo.GetAll(cancellationToken);
    }

    [HttpGet("permissions")]
    [Permission(Permissions.RolesRead)]
    public async Task<Response> GetPermissions()
    {
        return await _roleRepo.GetPermissions();
    }

    [HttpGet("{id}")]
    [Permission(Permissions.RolesRead)]
    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        return await _roleRepo.View(id, cancellationToken);
    }

    [HttpPost]
    [Permission(Permissions.RolesManage)]
    public async Task<Response> Add(AddRoleModel model, CancellationToken cancellationToken)
    {
        return await _roleRepo.Add(model, cancellationToken);
    }

    [HttpPut]
    [Permission(Permissions.RolesManage)]
    public async Task<Response> Update(UpdateRoleModel model, CancellationToken cancellationToken)
    {
        return await _roleRepo.Update(model, cancellationToken);
    }

    [HttpDelete("{id}")]
    [Permission(Permissions.RolesManage)]
    public async Task<Response> Delete(long id, CancellationToken cancellationToken)
    {
        return await _roleRepo.Delete(id, cancellationToken);
    }
}
