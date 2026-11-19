namespace Core.DTO.Billing;

/// <summary>A shift with its money totals (computed live while open, stored when closed).</summary>
public class CashierShiftDto
{
    public long Id { get; set; }
    public long BranchId { get; set; }
    public long CashierUserId { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal CardIn { get; set; }
    public decimal WalletIn { get; set; }
    public decimal NonCashOut { get; set; }
    public int PaymentsCount { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? Variance { get; set; }
    public long? ClosedByUserId { get; set; }
    public string? Notes { get; set; }
}
