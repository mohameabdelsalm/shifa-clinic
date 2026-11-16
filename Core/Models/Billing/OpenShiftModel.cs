namespace Core.Models.Billing;

public class OpenShiftModel
{
    public long BranchId { get; set; }

    /// <summary>Cash in the drawer when the shift starts.</summary>
    public decimal OpeningCash { get; set; }
}
