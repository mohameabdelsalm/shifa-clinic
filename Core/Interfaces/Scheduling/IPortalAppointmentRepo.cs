using Core.Common;
using Core.Models.Scheduling;

namespace Core.Interfaces.Scheduling;

public interface IPortalAppointmentRepo
{
    Task<Response> GetAvailability(AvailabilityModel model, CancellationToken cancellationToken);
    Task<Response> Book(PortalBookAppointmentModel model, string? idempotencyKey, CancellationToken cancellationToken);
    Task<Response> GetMine(CancellationToken cancellationToken);
    Task<Response> Cancel(long id, CancelAppointmentModel model, CancellationToken cancellationToken);
}
