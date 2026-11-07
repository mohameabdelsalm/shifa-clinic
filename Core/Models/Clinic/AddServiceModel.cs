namespace Core.Models.Clinic;

public class AddServiceModel
{
    public long SpecialtyId { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int DurationMinutes { get; set; }
}
