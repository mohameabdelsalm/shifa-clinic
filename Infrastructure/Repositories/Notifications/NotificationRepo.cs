using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Notifications;
using Core.Entities.Notifications;
using Core.Enums.Notifications;
using Core.Interfaces.Common;
using Core.Interfaces.Notifications;
using Core.Models.Notifications;
using Infrastructure.Data;
using Infrastructure.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Notifications;

/// <summary>Staff view of messages to patients and the clinic's templates (ADR-015).</summary>
public class NotificationRepo : INotificationRepo
{
    private readonly NotificationsDbContext _context;
    private readonly NotificationTemplates _templates;
    private readonly IMapper _mapper;
    private readonly IAccessContext _access;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public NotificationRepo(NotificationsDbContext context, NotificationTemplates templates, IMapper mapper, IAccessContext access, IAuditService audit,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _templates = templates;
        _mapper = mapper;
        _access = access;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> Search(SearchNotificationsModel model, CancellationToken cancellationToken)
    {
        var query = SortNotifications(InScope(_context.Notifications.AsNoTracking()).Where(FilterNotifications(model)), model);
        var paged = await query.ProjectTo<NotificationDto>(_mapper.ConfigurationProvider).ToPagedListAsync(model, cancellationToken);
        foreach (var item in paged.Items)
            item.Recipient = Mask(item.Recipient);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    private static Expression<Func<Notification, bool>> FilterNotifications(SearchNotificationsModel model)
    {
        DateTimeOffset? fromUtc = model.FromDate is DateOnly from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        DateTimeOffset? toUtc = model.ToDate is DateOnly to ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        return n => (model.PatientId == null || n.PatientId == model.PatientId)
                    && (model.AppointmentId == null || n.AppointmentId == model.AppointmentId)
                    && (model.BranchId == null || n.BranchId == model.BranchId)
                    && (model.Status == null || n.Status == model.Status)
                    && (model.Kind == null || n.Kind == model.Kind)
                    && (fromUtc == null || n.ScheduledAt >= fromUtc)
                    && (toUtc == null || n.ScheduledAt < toUtc);
    }

    private static IQueryable<Notification> SortNotifications(IQueryable<Notification> query, SearchNotificationsModel model) => model.SortBy switch
    {
        "status" => query.OrderBy(model.SortType, n => n.Status).ThenBy(n => n.Id),
        "kind" => query.OrderBy(model.SortType, n => n.Kind).ThenBy(n => n.Id),
        _ => query.OrderBy(model.SortType, n => n.ScheduledAt).ThenBy(n => n.Id),
    };

    public async Task<Response> GetTemplates(CancellationToken cancellationToken)
    {
        var custom = await _context.NotificationTemplates.AsNoTracking().ToListAsync(cancellationToken);
        var result = new List<NotificationTemplateDto>();
        foreach (var kind in Enum.GetValues<NotificationKindEnum>())
        foreach (var channel in Enum.GetValues<NotificationChannelEnum>())
        foreach (var language in NotificationPlaceholders.Languages)
        {
            var own = custom.FirstOrDefault(t => t.Kind == kind && t.Channel == channel && t.Language == language);
            result.Add(new NotificationTemplateDto
            {
                Kind = kind.ToString(),
                Channel = channel.ToString(),
                Language = language,
                Body = own?.Body ?? _templates.DefaultBody(kind, channel, language),
                IsCustom = own is not null,
                Placeholders = NotificationPlaceholders.For(kind).Select(p => $"{{{p}}}").ToList(),
            });
        }
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: result);
    }

    public async Task<Response> SaveTemplate(SaveNotificationTemplateModel model, CancellationToken cancellationToken)
    {
        var existing = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Kind == model.Kind && t.Channel == model.Channel && t.Language == model.Language, cancellationToken);

        if (model.Body is null)
        {
            if (existing is not null)
                _context.NotificationTemplates.Remove(existing);
        }
        else if (existing is null)
            _context.NotificationTemplates.Add(new NotificationTemplate { Kind = model.Kind, Channel = model.Channel, Language = model.Language, Body = model.Body.Trim() });
        else
            existing.Body = model.Body.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Notifications.TemplateSaved", nameof(NotificationTemplate), existing?.Id,
            detail: $"{model.Kind} {model.Channel} {model.Language} {(model.Body is null ? "default" : "custom")}", cancellationToken: CancellationToken.None);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    /// <summary>Branch-limited staff see messages of their branches only.</summary>
    private IQueryable<Notification> InScope(IQueryable<Notification> notifications)
    {
        if (_access.HasAllBranches)
            return notifications;
        var scope = _access.BranchIds.ToList();
        return notifications.Where(n => n.BranchId != null && scope.Contains(n.BranchId.Value));
    }

    /// <summary>"+201012345678" → "+2010*****678"; "name@example.com" → "n***@example.com".</summary>
    private static string Mask(string recipient)
    {
        var at = recipient.IndexOf('@');
        if (at > 0)
            return recipient[..1] + "***" + recipient[at..];
        return recipient.Length <= 7 ? "***" : recipient[..5] + new string('*', recipient.Length - 8) + recipient[^3..];
    }
}
