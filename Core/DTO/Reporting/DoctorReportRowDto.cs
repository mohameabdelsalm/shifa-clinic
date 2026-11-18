namespace Core.DTO.Reporting;

/// <summary>One doctor over the period (all branches in scope), or the totals row.</summary>
public class DoctorReportRowDto
{
    public long? DoctorId { get; set; }
    public string Doctor { get; set; } = "";
    public int Appointments { get; set; }
    public int Completed { get; set; }
    public int NoShows { get; set; }
    public int Cancelled { get; set; }
    public decimal NoShowRate { get; set; }

    /// <summary>Scheduled working minutes minus time off.</summary>
    public int AvailableMinutes { get; set; }

    /// <summary>Minutes of appointments that kept their time (not cancelled, not no-show).</summary>
    public int BookedMinutes { get; set; }

    /// <summary>Booked / available, percent.</summary>
    public decimal Utilization { get; set; }
}
