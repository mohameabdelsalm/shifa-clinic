using System.Text.Json.Serialization;
using Core.Enums.Patients;

namespace Core.Models.Patients;

public class PatientIdentifierModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IdentifierTypeEnum Type { get; set; }
    public string Value { get; set; } = "";
}
