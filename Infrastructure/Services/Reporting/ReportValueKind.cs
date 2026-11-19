namespace Infrastructure.Services.Reporting;

public enum ReportValueKind
{
    Text,
    Integer,
    Money,

    /// <summary>A number already in percent (12.5 = 12.5 %).</summary>
    Percent,
}
