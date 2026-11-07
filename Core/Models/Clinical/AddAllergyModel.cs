using System.Text.Json.Serialization;
using Core.Enums.Clinical;

namespace Core.Models.Clinical;

public class AddAllergyModel
{
    public long PatientId { get; set; }
    public string Allergen { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AllergySeverityEnum Severity { get; set; }

    public string? Reaction { get; set; }
}
