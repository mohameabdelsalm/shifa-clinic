namespace Core.DTO.Clinic;

public class ServiceDto
{
    public long Id { get; set; }
    public long SpecialtyId { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";

    /// <summary>Current prices, only for the branches the caller may see.</summary>
    public List<ServicePriceDto> Prices { get; set; } = [];
}
