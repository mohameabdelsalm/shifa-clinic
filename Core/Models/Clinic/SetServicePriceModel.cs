namespace Core.Models.Clinic;

public class SetServicePriceModel
{
    public long ServiceId { get; set; }
    public long BranchId { get; set; }
    public decimal Price { get; set; }
}
