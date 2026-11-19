namespace Core.DTO.Dashboard;

/// <summary>One day (periods up to 31 days) or one month of the timeline.</summary>
public class TimelinePointDto
{
    /// <summary>"2026-11-03" for a day, "2026-11" for a month.</summary>
    public string Period { get; set; } = "";
    public string Label { get; set; } = "";
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public int Appointments { get; set; }
    public int Completed { get; set; }
    public int NoShows { get; set; }
    public int Cancelled { get; set; }
    public int NewPatients { get; set; }
}
