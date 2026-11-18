namespace Core.DTO.Reporting;

/// <summary>A report for the screen: the rows and a totals row over the whole period.</summary>
public class ReportDto<TRow>
{
    public string Title { get; set; } = "";
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public DateTimeOffset GeneratedAt { get; set; }
    public List<TRow> Rows { get; set; } = [];
    public TRow? Totals { get; set; }
}
