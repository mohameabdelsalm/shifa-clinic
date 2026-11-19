namespace Core.DTO.Dashboard;

/// <summary>Totals of the period for one branch or one doctor.</summary>
public class BreakdownRowDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Appointments { get; set; }
    public int Completed { get; set; }
    public int NoShows { get; set; }
    public int Cancelled { get; set; }
    public decimal NoShowRate { get; set; }
}
