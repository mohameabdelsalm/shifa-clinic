namespace Core.DTO.Clinic;

public class ServicePriceDto
{
    public long BranchId { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
}
