namespace Core.DTO.Reporting;

/// <summary>Revenue (what was billed) against collections (money in/out) on one local day, or the totals row.</summary>
public class RevenueReportRowDto
{
    public string Date { get; set; } = "";
    public decimal Invoiced { get; set; }
    public decimal Credited { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cash { get; set; }
    public decimal Card { get; set; }
    public decimal Wallet { get; set; }
    public decimal Online { get; set; }
    public decimal Refunded { get; set; }
    public decimal Collections { get; set; }

    /// <summary>Revenue − collections: positive = billed but not (yet) collected.</summary>
    public decimal Difference { get; set; }
}
