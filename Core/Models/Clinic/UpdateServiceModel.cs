namespace Core.Models.Clinic;

public class UpdateServiceModel
{
    public long Id { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int DurationMinutes { get; set; }
}
