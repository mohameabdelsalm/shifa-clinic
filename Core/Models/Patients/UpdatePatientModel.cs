using System.Text.Json.Serialization;
using Core.Enums.Patients;

namespace Core.Models.Patients;

public class UpdatePatientModel
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GenderEnum Gender { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    /// <summary>The patient's full list of identifiers; missing ones are removed.</summary>
    public List<PatientIdentifierModel> Identifiers { get; set; } = [];
}
