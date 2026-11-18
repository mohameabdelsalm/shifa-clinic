using Core.Entities.Common;
using Core.Enums.Notifications;

namespace Core.Entities.Notifications;

/// <summary>
/// A clinic's own wording for one message kind, channel and language. Without a row the built-in default text
/// (resources) is used, so new clinics need no setup.
/// </summary>
public class NotificationTemplate : TenantOwnedEntity
{
    public NotificationKindEnum Kind { get; set; }
    public NotificationChannelEnum Channel { get; set; }
    public string Language { get; set; } = "ar";
    public string Body { get; set; } = "";
}
