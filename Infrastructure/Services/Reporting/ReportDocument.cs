namespace Infrastructure.Services.Reporting;

/// <summary>A report ready to write to Excel or PDF: localized texts, typed cells (numbers stay numbers).</summary>
public sealed class ReportDocument
{
    public string Clinic { get; init; } = "";
    public string Title { get; init; } = "";

    /// <summary>"Period: … – …".</summary>
    public string Period { get; init; } = "";

    /// <summary>"Generated: …".</summary>
    public string Generated { get; init; } = "";
    public string TotalLabel { get; init; } = "";
    public bool RightToLeft { get; init; }
    public IReadOnlyList<(string Header, ReportValueKind Kind)> Columns { get; init; } = [];
    public IReadOnlyList<object?[]> Rows { get; init; } = [];
    public object?[]? Totals { get; init; }
}
