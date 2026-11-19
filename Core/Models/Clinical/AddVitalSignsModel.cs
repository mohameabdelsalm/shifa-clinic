namespace Core.Models.Clinical;

public class AddVitalSignsModel
{
    public long VisitId { get; set; }
    public int? BpSystolic { get; set; }
    public int? BpDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public int? OxygenSaturation { get; set; }
}
