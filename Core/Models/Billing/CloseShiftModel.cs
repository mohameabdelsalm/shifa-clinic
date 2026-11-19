namespace Core.Models.Billing;

public class CloseShiftModel
{
    public long ShiftId { get; set; }

    /// <summary>Cash counted in the drawer at closing.</summary>
    public decimal CountedCash { get; set; }
    public string? Notes { get; set; }
}
