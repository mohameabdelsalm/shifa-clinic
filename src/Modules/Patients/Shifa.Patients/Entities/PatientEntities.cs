using Shifa.Core.Entities.Common;
using Shifa.Patients.Enums;

namespace Shifa.Patients.Entities;

/// <summary>
/// A person receiving care at one tenant. The same person at two tenants has two independent profiles (assumption A1).
/// Login accounts are separate (Identity); a profile is reached by a portal user through <see cref="PatientAccountAccess"/>.
/// </summary>
public class Patient : TenantOwnedEntity
{
    /// <summary>The running number inside the tenant; <see cref="Mrn"/> is derived from it.</summary>
    public int Number { get; set; }

    /// <summary>Medical record number, unique inside the tenant (e.g. MRN-000123).</summary>
    public string Mrn { get; set; } = "";

    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public GenderEnum Gender { get; set; }

    /// <summary>Normalized (see PhoneNumbers). The contact number; portal access is granted separately.</summary>
    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
    public string? Address { get; set; }

    /// <summary>Maintained by Scheduling's no-show job (Phase 5/8).</summary>
    public int NoShowCount { get; set; }

    public static string FormatMrn(int number) => $"MRN-{number:D6}";

    public bool IsMinor(DateOnly today) => DateOfBirth.AddYears(18) > today;
}

public class PatientIdentifier : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public IdentifierTypeEnum Type { get; set; }

    /// <summary>National ID or passport number. Sensitive: never logged.</summary>
    public string Value { get; set; } = "";
}

/// <summary>Contact details of a guardian (informational). Portal access for a guardian is a <see cref="PatientAccountAccess"/>.</summary>
public class PatientGuardian : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Lets whoever proves ownership of <see cref="PhoneNumber"/> (OTP) act for the patient in the portal.
/// One parent's phone can be linked to several children. Linked by phone number, not by user id, so a
/// profile can be prepared before the person ever signs in and Identity data stays separate from medical data.
/// </summary>
public class PatientAccountAccess : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public string PhoneNumber { get; set; } = "";
    public RelationshipEnum Relationship { get; set; }
    public bool CanBook { get; set; } = true;
}

public class PatientConsent : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public ConsentTypeEnum ConsentType { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsActive => RevokedAt is null;
}
