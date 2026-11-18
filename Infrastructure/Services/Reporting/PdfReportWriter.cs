using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services.Reporting;

/// <summary>
/// A4 landscape PDF with QuestPDF: header (clinic, title, period), the table with a bold totals row, and a footer with
/// the generation time and page numbers. Arabic is laid out right-to-left. The fonts must be able to draw Arabic
/// (Linux images need an Arabic font deployed in Shifa/Fonts or installed, ADR-016).
/// </summary>
public static class PdfReportWriter
{
    /// <summary>
    /// Font families in fallback order, set once at startup (Reports:PdfFontFamilies). QuestPDF refuses to draw when a
    /// listed family isn't installed or deployed, so list only fonts that exist where the API runs.
    /// </summary>
    public static string[] FontFamilies { get; set; } = OperatingSystem.IsWindows() ? ["Arial", "Tahoma"] : ["Noto Naskh Arabic"];

    public static byte[] Write(ReportDocument document) =>
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontSize(9).FontFamily(FontFamilies));
            if (document.RightToLeft)
                page.ContentFromRightToLeft();

            page.Header().PaddingBottom(8).Column(column =>
            {
                column.Item().Text(document.Clinic).FontSize(15).Bold();
                column.Item().Text(document.Title).FontSize(12).SemiBold();
                column.Item().Text(document.Period).FontSize(9).FontColor(Colors.Grey.Darken1);
            });

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in document.Columns)
                        columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    foreach (var (title, kind) in document.Columns)
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignmentFor(kind).Text(title).SemiBold();
                });

                var striped = false;
                foreach (var row in document.Rows)
                {
                    for (var c = 0; c < document.Columns.Count; c++)
                    {
                        var cell = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                        if (striped)
                            cell = cell.Background(Colors.Grey.Lighten5);
                        cell.Padding(4).AlignmentFor(document.Columns[c].Kind).Text(Format(row[c], document.Columns[c].Kind));
                    }
                    striped = !striped;
                }

                if (document.Totals is not null)
                {
                    for (var c = 0; c < document.Columns.Count; c++)
                    {
                        table.Cell().BorderTop(1).BorderColor(Colors.Grey.Darken1).Padding(4).AlignmentFor(document.Columns[c].Kind)
                            .Text(c == 0 && document.Totals[c] is null ? document.TotalLabel : Format(document.Totals[c], document.Columns[c].Kind)).Bold();
                    }
                }
            });

            page.Footer().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(document.Generated).FontSize(8).FontColor(Colors.Grey.Darken1);
                row.AutoItem().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();

    private static IContainer AlignmentFor(this IContainer container, ReportValueKind kind) =>
        kind == ReportValueKind.Text ? container : container.AlignRight();

    private static string Format(object? value, ReportValueKind kind) => value switch
    {
        null => "",
        decimal d when kind == ReportValueKind.Percent => d.ToString("0.0", CultureInfo.InvariantCulture) + "%",
        decimal d => d.ToString("#,##0.00", CultureInfo.InvariantCulture),
        int i => i.ToString("#,##0", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
