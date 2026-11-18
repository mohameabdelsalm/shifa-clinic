using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Notifications;
using Core.Models.Notifications;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Notifications;

/// <summary>Messages sent (or planned) to patients: confirmations, reminders, receipts (ADR-015).</summary>
[ApiController]
[RequireFeature(FeatureCodes.Notifications)]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationRepo _repo;

    public NotificationController(INotificationRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("search")]
    [Permission(Permissions.NotificationsRead)]
    public async Task<Response> Search(SearchNotificationsModel model, CancellationToken ct) => await _repo.Search(model, ct);
}
