using Core.Common;
using Core.Models.Scheduling;

namespace Core.Interfaces.Scheduling;

public interface IAppointmentRepo
{
    Task<Response> GetAvailability(AvailabilityModel model, CancellationToken cancellationToken);
    Task<Response> Book(BookAppointmentModel model, string? idempotencyKey, CancellationToken cancellationToken);
    Task<Response> Search(SearchAppointmentsModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Confirm(long id, CancellationToken cancellationToken);
    Task<Response> CheckIn(long id, CancellationToken cancellationToken);
    Task<Response> Cancel(long id, CancelAppointmentModel model, CancellationToken cancellationToken);
    Task<Response> MarkNoShow(long id, CancellationToken cancellationToken);
    Task<Response> Reschedule(RescheduleAppointmentModel model, CancellationToken cancellationToken);
}
