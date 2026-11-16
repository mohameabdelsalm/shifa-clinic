namespace Core.DTO.Dashboard;

public class MoneyPointDto
{
    public string Period { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal Cash { get; set; }
    public decimal Card { get; set; }
    public decimal Wallet { get; set; }
    public decimal Online { get; set; }

    /// <summary>All payments (Cash + Card + Wallet + Online).</summary>
    public decimal Collected { get; set; }
    public decimal Refunded { get; set; }

    /// <summary>Collected − Refunded.</summary>
    public decimal Net { get; set; }
    public int PaymentsCount { get; set; }
}
