using Core.Common;
using Core.Models.Dashboard;

namespace Core.Interfaces.Dashboard;

public interface IDashboardRepo
{
    Task<Response> Statistics(DashboardModel model, CancellationToken cancellationToken);
    Task<Response> Timeline(DashboardModel model, CancellationToken cancellationToken);
    Task<Response> Branches(DashboardModel model, CancellationToken cancellationToken);
    Task<Response> Doctors(DashboardModel model, CancellationToken cancellationToken);
}
