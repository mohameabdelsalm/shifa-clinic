namespace Infrastructure.Services.Reporting;

/// <summary>One column of an exported report: header resource key, value of a row, and how to format it.</summary>
public sealed record ReportColumn<TRow>(string HeaderKey, Func<TRow, object?> Value, ReportValueKind Kind = ReportValueKind.Text);
