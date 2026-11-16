namespace Core.DTO.Dashboard;

/// <summary>One number of the period, compared with the same period a year earlier (like HRM).</summary>
public class KpiDto
{
    public decimal Total { get; set; }
    public decimal Previous { get; set; }

    /// <summary>The comparison period, e.g. "Nov 2025" or "Oct 2025 - Nov 2025".</summary>
    public string VsPeriod { get; set; } = "";

    /// <summary>Change in percent (1 decimal); 100 when the previous value was 0 and the current isn't.</summary>
    public decimal ChangePercent { get; set; }

    /// <summary>True when the change is good news (for no-show rate and cancellations, lower is better).</summary>
    public bool IsPositive { get; set; }
}
