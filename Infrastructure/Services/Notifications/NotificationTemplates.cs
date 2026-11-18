using System.Globalization;
using System.Text.RegularExpressions;
using Core;
using Core.Enums.Notifications;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Services.Notifications;

/// <summary>
/// The text of a message: the clinic's own template if it has one, else the built-in default from the resources
/// (keys "Notification.{Kind}.{Channel}" and "Notification.{Kind}.Subject"). Placeholders {name} are replaced from the
/// payload; unknown ones become empty.
/// </summary>
public sealed partial class NotificationTemplates
{
    private readonly NotificationsDbContext _context;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public NotificationTemplates(NotificationsDbContext context, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<string?> CustomBodyAsync(NotificationKindEnum kind, NotificationChannelEnum channel, string language, CancellationToken cancellationToken) =>
        await _context.NotificationTemplates.AsNoTracking()
            .Where(t => t.Kind == kind && t.Channel == channel && t.Language == language)
            .Select(t => t.Body)
            .FirstOrDefaultAsync(cancellationToken);

    public string DefaultBody(NotificationKindEnum kind, NotificationChannelEnum channel, string language) =>
        InLanguage(language, () => _localizer[$"Notification.{kind}.{channel}"].Value);

    public string Subject(NotificationKindEnum kind, string language) =>
        InLanguage(language, () => _localizer[$"Notification.{kind}.Subject"].Value);

    public async Task<string> RenderAsync(NotificationKindEnum kind, NotificationChannelEnum channel, string language,
        IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken)
    {
        var body = await CustomBodyAsync(kind, channel, language, cancellationToken) ?? DefaultBody(kind, channel, language);
        return Placeholder().Replace(body, m => values.TryGetValue(m.Groups[1].Value, out var value) ? value : "");
    }

    private static string InLanguage(string language, Func<string> read)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            return read();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [GeneratedRegex(@"\{([A-Za-z]+)\}")]
    private static partial Regex Placeholder();
}
