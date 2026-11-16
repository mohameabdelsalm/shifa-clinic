namespace Core.DTO.Dashboard;

/// <summary>Collections of the period: totals and a timeline per day (≤ 31 days) or month, split by payment method.</summary>
public class PaymentsDashboardDto
{
    public MoneyPointDto Totals { get; set; } = new();
    public List<MoneyPointDto> Timeline { get; set; } = [];
}
