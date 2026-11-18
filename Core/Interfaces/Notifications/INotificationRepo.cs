using Core.Common;
using Core.Models.Notifications;

namespace Core.Interfaces.Notifications;

public interface INotificationRepo
{
    Task<Response> Search(SearchNotificationsModel model, CancellationToken cancellationToken);
    Task<Response> GetTemplates(CancellationToken cancellationToken);
    Task<Response> SaveTemplate(SaveNotificationTemplateModel model, CancellationToken cancellationToken);
}
