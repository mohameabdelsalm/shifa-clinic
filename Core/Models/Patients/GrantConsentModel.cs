using System.Text.Json.Serialization;
using Core.Enums.Patients;

namespace Core.Models.Patients;

public class GrantConsentModel
{
    public long PatientId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ConsentTypeEnum ConsentType { get; set; }
}
