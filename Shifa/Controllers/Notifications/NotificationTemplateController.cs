using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Notifications;
using Core.Models.Notifications;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Notifications;

/// <summary>The clinic's message texts; each kind/channel/language falls back to the built-in default.</summary>
[ApiController]
[RequireFeature(FeatureCodes.Notifications)]
[Route("api/notification-templates")]
public class NotificationTemplateController : ControllerBase
{
    private readonly INotificationRepo _repo;

    public NotificationTemplateController(INotificationRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [Permission(Permissions.NotificationsRead)]
    public async Task<Response> Get(CancellationToken ct) => await _repo.GetTemplates(ct);

    /// <summary>Sets the clinic's own text (placeholders like {doctor} only); Body = null restores the default.</summary>
    [HttpPut]
    [Permission(Permissions.NotificationsManage)]
    public async Task<Response> Save(SaveNotificationTemplateModel model, CancellationToken ct) => await _repo.SaveTemplate(model, ct);
}
