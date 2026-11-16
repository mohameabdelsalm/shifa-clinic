using Core.Common;
using Core.Interfaces.Identity;
using Core.Models.Identity;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Identity;

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

    /// <summary>Sends a new invitation link to a staff member who hasn't set a password yet.</summary>
    [HttpPost("{id}/resend-invitation")]
    [Permission(Permissions.StaffManage)]
    public async Task<Response> ResendInvitation(long id, CancellationToken cancellationToken)
    {
        return await _staffRepo.ResendInvitation(id, cancellationToken);
    }
}
