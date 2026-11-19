using System.Text.Json.Serialization;
using Core.Enums.Patients;

namespace Core.Models.Patients;

public class LinkPortalAccessModel
{
    public long PatientId { get; set; }
    public string PhoneNumber { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RelationshipEnum Relationship { get; set; }

    public bool CanBook { get; set; } = true;
}
