namespace Core.DTO.Notifications;

/// <summary>The text used for one kind/channel/language: the clinic's own, or the built-in default.</summary>
public class NotificationTemplateDto
{
    public string Kind { get; set; } = "";
    public string Channel { get; set; } = "";
    public string Language { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsCustom { get; set; }

    /// <summary>The placeholders this kind can use, e.g. {doctor}.</summary>
    public List<string> Placeholders { get; set; } = [];
}
