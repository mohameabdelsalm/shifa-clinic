using System.Text.Json.Serialization;
using Core.Enums.Patients;

namespace Core.Models.Patients;

public class AddPatientModel
{
    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GenderEnum Gender { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public List<PatientIdentifierModel> Identifiers { get; set; } = [];

    /// <summary>At least one is required when the patient is under 18.</summary>
    public List<PatientGuardianModel> Guardians { get; set; } = [];
}
