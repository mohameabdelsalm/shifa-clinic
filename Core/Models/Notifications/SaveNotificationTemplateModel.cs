using System.Text.Json.Serialization;
using Core.Enums.Notifications;

namespace Core.Models.Notifications;

/// <summary>Sets the clinic's own text; Body = null goes back to the built-in default.</summary>
public class SaveNotificationTemplateModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationKindEnum Kind { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationChannelEnum Channel { get; set; }

    public string Language { get; set; } = "ar";
    public string? Body { get; set; }
}
