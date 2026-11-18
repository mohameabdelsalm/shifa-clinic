using ClosedXML.Excel;

namespace Infrastructure.Services.Reporting;

/// <summary>
/// One worksheet: clinic, title, period and generation time on top, then a header row, the rows and a bold totals row.
/// Numbers are written as numbers (sums and filters work in Excel); text is written as text, never as a formula.
/// </summary>
public static class ExcelReportWriter
{
    private const int HeaderRow = 5;

    public static byte[] Write(ReportDocument document)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName(document.Title));
        sheet.RightToLeft = document.RightToLeft;

        sheet.Cell(1, 1).Value = document.Clinic;
        sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        sheet.Cell(2, 1).Value = document.Title;
        sheet.Cell(2, 1).Style.Font.SetBold().Font.SetFontSize(12);
        sheet.Cell(3, 1).Value = $"{document.Period}    {document.Generated}";

        for (var c = 0; c < document.Columns.Count; c++)
        {
            var cell = sheet.Cell(HeaderRow, c + 1);
            cell.Value = document.Columns[c].Header;
            cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8EEF4"));
            cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
        }

        var row = HeaderRow + 1;
        foreach (var values in document.Rows)
            WriteRow(sheet, row++, document, values, bold: false);
        if (document.Totals is not null)
        {
            WriteRow(sheet, row, document, document.Totals, bold: true);
            sheet.Row(row).Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
        }

        sheet.SheetView.FreezeRows(HeaderRow);
        sheet.Columns(1, document.Columns.Count).AdjustToContents(HeaderRow, row);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteRow(IXLWorksheet sheet, int row, ReportDocument document, object?[] values, bool bold)
    {
        for (var c = 0; c < document.Columns.Count; c++)
        {
            var cell = sheet.Cell(row, c + 1);
            var value = values[c];
            switch (value)
            {
                case null:
                    break;
                case int i:
                    cell.Value = i;
                    break;
                case long l:
                    cell.Value = l;
                    break;
                case decimal d:
                    cell.Value = d;
                    cell.Style.NumberFormat.Format = document.Columns[c].Kind == ReportValueKind.Percent ? "0.0" : "#,##0.00";
                    break;
                default:
                    cell.Value = value.ToString();   // plain text: never interpreted as a formula
                    break;
            }
            if (bold)
                cell.Style.Font.SetBold();
        }
    }

    /// <summary>Excel sheet names: at most 31 characters, none of : \ / ? * [ ].</summary>
    private static string SheetName(string title)
    {
        var clean = new string(title.Where(ch => !":\\/?*[]".Contains(ch)).ToArray()).Trim();
        return clean.Length == 0 ? "Report" : clean.Length > 31 ? clean[..31] : clean;
    }
}
