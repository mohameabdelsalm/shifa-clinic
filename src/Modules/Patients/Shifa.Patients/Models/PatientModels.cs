using System.Text.Json.Serialization;
using Shifa.Core.Common;
using Shifa.Patients.Enums;

namespace Shifa.Patients.Models;

public class PatientIdentifierModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IdentifierTypeEnum Type { get; set; }
    public string Value { get; set; } = "";
}

public class PatientGuardianModel
{
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}

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

public class SearchPatientsModel : PaginationParameters
{
    /// <summary>Name, MRN, phone number, or an exact national ID / passport number.</summary>
    public string? Text { get; set; }
}

public class AddGuardianModel
{
    public long PatientId { get; set; }
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}

public class GrantConsentModel
{
    public long PatientId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ConsentTypeEnum ConsentType { get; set; }
}

public class LinkPortalAccessModel
{
    public long PatientId { get; set; }
    public string PhoneNumber { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RelationshipEnum Relationship { get; set; }

    public bool CanBook { get; set; } = true;
}
