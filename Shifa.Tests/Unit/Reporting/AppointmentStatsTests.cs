using Core.DTO.Scheduling;
using Core.Enums.Scheduling;
using Infrastructure.Services.Reporting;

namespace Shifa.Tests.Unit.Reporting;

/// <summary>The definitions shared by the dashboard and the reports (ADR-016).</summary>
public class AppointmentStatsTests
{
    private static readonly DateOnly Day = new(2026, 11, 18);

    private static AppointmentCountRow Row(AppointmentStatusEnum status, int count, int minutes = 0) => new(Day, 1, 1, status, count, minutes);

    [Fact]
    public void Booked_excludes_cancelled_and_no_show_rate_uses_patients_expected()
    {
        var s = AppointmentStats.Summarize(
        [
            Row(AppointmentStatusEnum.Completed, 6, 180),
            Row(AppointmentStatusEnum.CheckedIn, 1, 30),
            Row(AppointmentStatusEnum.Confirmed, 2, 60),
            Row(AppointmentStatusEnum.NoShow, 3, 90),
            Row(AppointmentStatusEnum.Cancelled, 4, 120),
        ]);

        Assert.Equal(12, s.Booked);              // 16 - 4 cancelled
        Assert.Equal(6, s.Completed);
        Assert.Equal(3, s.NoShows);
        Assert.Equal(4, s.Cancelled);
        Assert.Equal(30.0m, s.NoShowRate);       // 3 / (7 came + 3)
        Assert.Equal(270, s.BookedMinutes);      // kept their time: completed, checked in, confirmed
    }

    [Fact]
    public void No_one_expected_means_a_zero_rate_not_a_division_error()
    {
        var s = AppointmentStats.Summarize([Row(AppointmentStatusEnum.Confirmed, 2), Row(AppointmentStatusEnum.Cancelled, 1)]);
        Assert.Equal(0, s.NoShowRate);
        Assert.Equal(2, s.Booked);
    }
}
