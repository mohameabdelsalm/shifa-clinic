namespace Shifa.Patients.DTO;

public class PatientIdentifierDto
{
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
}

public class PatientGuardianDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}

public class PatientConsentDto
{
    public long Id { get; set; }
    public string ConsentType { get; set; } = "";
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public class PatientPortalAccessDto
{
    public long Id { get; set; }
    public string PhoneNumber { get; set; } = "";
    public string Relationship { get; set; } = "";
    public bool CanBook { get; set; }
}

/// <summary>A row of the search list.</summary>
public class SearchPatientsDto
{
    public long Id { get; set; }
    public string Mrn { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string? PhoneNumber { get; set; }
}

/// <summary>The full profile, for staff with Patients.Read.</summary>
public class PatientDto : SearchPatientsDto
{
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int NoShowCount { get; set; }
    public List<PatientIdentifierDto> Identifiers { get; set; } = [];
    public List<PatientGuardianDto> Guardians { get; set; } = [];
    public List<PatientConsentDto> Consents { get; set; } = [];
    public List<PatientPortalAccessDto> PortalAccess { get; set; } = [];
}

/// <summary>What a portal user sees of a patient they act for: no identifiers, no internal counters.</summary>
public class PortalPatientDto
{
    public long Id { get; set; }
    public string Mrn { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string Relationship { get; set; } = "";
    public bool CanBook { get; set; }
}
