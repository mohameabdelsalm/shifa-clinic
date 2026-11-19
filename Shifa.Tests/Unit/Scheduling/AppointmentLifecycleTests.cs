using Core.Entities.Scheduling;
using Core.Enums.Scheduling;

namespace Shifa.Tests.Unit.Scheduling;

public class AppointmentLifecycleTests
{
    private static Appointment InStatus(AppointmentStatusEnum status)
    {
        var appointment = new Appointment();
        appointment.Book(AppointmentStatusEnum.Confirmed);
        switch (status)
        {
            case AppointmentStatusEnum.Pending:
                appointment.Book(AppointmentStatusEnum.Pending);
                break;
            case AppointmentStatusEnum.Confirmed:
                break;
            case AppointmentStatusEnum.InProgress:
                appointment.MoveTo(AppointmentStatusEnum.CheckedIn);
                appointment.MoveTo(AppointmentStatusEnum.InProgress);
                break;
            case AppointmentStatusEnum.Completed:
                appointment.MoveTo(AppointmentStatusEnum.CheckedIn);
                appointment.MoveTo(AppointmentStatusEnum.InProgress);
                appointment.MoveTo(AppointmentStatusEnum.Completed);
                break;
            default:
                appointment.MoveTo(status);
                break;
        }

        Assert.Equal(status, appointment.Status);
        return appointment;
    }

    [Theory]
    [InlineData(AppointmentStatusEnum.Pending, AppointmentStatusEnum.Confirmed)]
    [InlineData(AppointmentStatusEnum.Pending, AppointmentStatusEnum.Cancelled)]
    [InlineData(AppointmentStatusEnum.Confirmed, AppointmentStatusEnum.CheckedIn)]
    [InlineData(AppointmentStatusEnum.Confirmed, AppointmentStatusEnum.Cancelled)]
    [InlineData(AppointmentStatusEnum.Confirmed, AppointmentStatusEnum.NoShow)]
    [InlineData(AppointmentStatusEnum.CheckedIn, AppointmentStatusEnum.InProgress)]
    [InlineData(AppointmentStatusEnum.InProgress, AppointmentStatusEnum.Completed)]
    public void Allowed_transitions_change_the_status(AppointmentStatusEnum from, AppointmentStatusEnum to)
    {
        var appointment = InStatus(from);

        Assert.Equal(TransitionResult.Changed, appointment.MoveTo(to));
        Assert.Equal(to, appointment.Status);
    }

    [Theory]
    [InlineData(AppointmentStatusEnum.Pending, AppointmentStatusEnum.CheckedIn)]
    [InlineData(AppointmentStatusEnum.Pending, AppointmentStatusEnum.NoShow)]
    [InlineData(AppointmentStatusEnum.CheckedIn, AppointmentStatusEnum.Cancelled)]
    [InlineData(AppointmentStatusEnum.CheckedIn, AppointmentStatusEnum.NoShow)]
    [InlineData(AppointmentStatusEnum.Cancelled, AppointmentStatusEnum.Confirmed)]
    [InlineData(AppointmentStatusEnum.Cancelled, AppointmentStatusEnum.CheckedIn)]
    [InlineData(AppointmentStatusEnum.NoShow, AppointmentStatusEnum.CheckedIn)]
    [InlineData(AppointmentStatusEnum.Completed, AppointmentStatusEnum.Cancelled)]
    public void Other_transitions_are_refused_and_keep_the_status(AppointmentStatusEnum from, AppointmentStatusEnum to)
    {
        var appointment = InStatus(from);

        Assert.Equal(TransitionResult.NotAllowed, appointment.MoveTo(to));
        Assert.Equal(from, appointment.Status);
    }

    [Fact]
    public void Repeating_the_current_status_is_a_no_op()
    {
        var appointment = InStatus(AppointmentStatusEnum.Cancelled);

        Assert.Equal(TransitionResult.Unchanged, appointment.MoveTo(AppointmentStatusEnum.Cancelled));
    }

    [Theory]
    [InlineData(AppointmentStatusEnum.CheckedIn)]
    [InlineData(AppointmentStatusEnum.Cancelled)]
    [InlineData(AppointmentStatusEnum.Completed)]
    public void A_new_appointment_starts_pending_or_confirmed_only(AppointmentStatusEnum status)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Appointment().Book(status));
    }
}
