namespace Core.Models.Dashboard;

public class PaymentsDashboardModel : DashboardModel
{
    /// <summary>Only money collected or refunded by this staff user.</summary>
    public long? CashierUserId { get; set; }
}
