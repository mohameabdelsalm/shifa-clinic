namespace Core.Models.Scheduling;

public class RescheduleAppointmentModel
{
    public long Id { get; set; }
    public DateTimeOffset StartAt { get; set; }
}
