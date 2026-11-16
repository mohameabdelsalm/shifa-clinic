namespace Core.DTO.Dashboard;

public class DashboardStatisticsDto
{
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";

    /// <summary>Booked appointments that weren't cancelled.</summary>
    public KpiDto Appointments { get; set; } = new();
    public KpiDto CompletedVisits { get; set; } = new();
    public KpiDto Cancellations { get; set; } = new();

    /// <summary>No-shows ÷ (no-shows + patients who came), in percent.</summary>
    public KpiDto NoShowRate { get; set; } = new();

    /// <summary>Invoiced − credited in the period (Billing).</summary>
    public KpiDto Revenue { get; set; } = new();

    /// <summary>Payments − refunds in the period (Billing).</summary>
    public KpiDto Collections { get; set; } = new();

    /// <summary>Patients registered in the period (whole organization; patients don't belong to a branch).</summary>
    public KpiDto NewPatients { get; set; } = new();
}
