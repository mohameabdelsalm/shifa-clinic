using Core.Common;

namespace Core.DTO.Reporting;

/// <summary>Either the JSON response or a file (Excel/PDF) to download.</summary>
public sealed class ReportOutput
{
    public Response? Json { get; init; }
    public byte[]? Content { get; init; }
    public string ContentType { get; init; } = "";
    public string FileName { get; init; } = "";
}
