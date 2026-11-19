using Core.DTO.Scheduling;
using Core.Enums.Scheduling;

namespace Infrastructure.Services.Reporting;

/// <summary>
/// The appointment counts shared by the dashboard and the reports, so both always agree:
/// <list type="bullet">
/// <item>Booked = all minus cancelled.</item>
/// <item>No-show rate = no-shows / (patients who came + no-shows), percent with one decimal.</item>
/// <item>Booked minutes = minutes of appointments that kept their time (not cancelled, not no-show).</item>
/// </list>
/// </summary>
public static class AppointmentStats
{
    public sealed record Summary(int Booked, int Completed, int NoShows, int Cancelled, decimal NoShowRate, int BookedMinutes);

    public static Summary Summarize(IEnumerable<AppointmentCountRow> rows)
    {
        var list = rows as IReadOnlyCollection<AppointmentCountRow> ?? rows.ToList();
        int Count(params AppointmentStatusEnum[] statuses) => list.Where(r => statuses.Contains(r.Status)).Sum(r => r.Count);

        var cancelled = Count(AppointmentStatusEnum.Cancelled);
        var noShows = Count(AppointmentStatusEnum.NoShow);
        var came = Count(AppointmentStatusEnum.CheckedIn, AppointmentStatusEnum.InProgress, AppointmentStatusEnum.Completed);
        var noShowRate = came + noShows == 0 ? 0 : Math.Round(noShows * 100m / (came + noShows), 1);
        var bookedMinutes = list.Where(r => r.Status is not (AppointmentStatusEnum.Cancelled or AppointmentStatusEnum.NoShow)).Sum(r => r.Minutes);

        return new Summary(list.Sum(r => r.Count) - cancelled, Count(AppointmentStatusEnum.Completed), noShows, cancelled, noShowRate, bookedMinutes);
    }
}
