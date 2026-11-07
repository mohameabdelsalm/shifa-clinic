using Core.Entities.Common;

namespace Core.Entities.Clinical;

/// <summary>One set of measurements taken during a visit (a nurse and then the doctor may each record one).</summary>
public class VitalSign : TenantOwnedEntity
{
    public long VisitId { get; set; }
    public int? BpSystolic { get; set; }
    public int? BpDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public int? OxygenSaturation { get; set; }
    public long RecordedByUserId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
