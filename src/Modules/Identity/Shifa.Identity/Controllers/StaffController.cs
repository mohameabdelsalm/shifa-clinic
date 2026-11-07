using Microsoft.AspNetCore.Mvc;
using Shifa.Core.Common;
using Shifa.Identity.Interfaces;
using Shifa.Identity.Models;
using Shifa.Infrastructure.Auth;

namespace Shifa.Identity.Controllers;

[ApiController]
[Route("api/staff")]
public class StaffController : ControllerBase
{
    private readonly IStaffRepo _staffRepo;

    public StaffController(IStaffRepo staffRepo)
    {
        _staffRepo = staffRepo;
    }

    [HttpPost("search")]
    [Permission(Permissions.StaffRead)]
    public async Task<Response> Search(SearchStaffModel model, CancellationToken cancellationToken)
    {
        return await _staffRepo.Search(model, cancellationToken);
    }

    [HttpGet("{id}")]
    [Permission(Permissions.StaffRead)]
    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        return await _staffRepo.View(id, cancellationToken);
    }

    [HttpPost]
    [Permission(Permissions.StaffManage)]
    public async Task<Response> Add(AddStaffModel model, CancellationToken cancellationToken)
    {
        return await _staffRepo.Add(model, cancellationToken);
    }

    [HttpPut]
    [Permission(Permissions.StaffManage)]
    public async Task<Response> Update(UpdateStaffModel model, CancellationToken cancellationToken)
    {
        return await _staffRepo.Update(model, cancellationToken);
    }

    [HttpPut("{id}/suspend")]
    [Permission(Permissions.StaffManage)]
    public async Task<Response> Suspend(long id, CancellationToken cancellationToken)
    {
        return await _staffRepo.Suspend(id, cancellationToken);
    }

    [HttpPut("{id}/activate")]
    [Permission(Permissions.StaffManage)]
    public async Task<Response> Activate(long id, CancellationToken cancellationToken)
    {
        return await _staffRepo.Activate(id, cancellationToken);
    }
}
