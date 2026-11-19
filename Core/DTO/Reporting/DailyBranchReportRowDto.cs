namespace Core.DTO.Reporting;

/// <summary>One branch on one local day (or the totals row: Date and Branch empty).</summary>
public class DailyBranchReportRowDto
{
    public string Date { get; set; } = "";
    public long? BranchId { get; set; }
    public string Branch { get; set; } = "";

    /// <summary>Booked, not cancelled.</summary>
    public int Appointments { get; set; }
    public int Completed { get; set; }
    public int NoShows { get; set; }
    public int Cancelled { get; set; }

    /// <summary>Percent of patients expected (came + no-shows) who didn't come.</summary>
    public decimal NoShowRate { get; set; }

    /// <summary>Invoiced − credited.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Payments − refunds.</summary>
    public decimal Collections { get; set; }
}
