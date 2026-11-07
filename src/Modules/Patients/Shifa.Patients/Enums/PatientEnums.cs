namespace Shifa.Patients.Enums;

public enum GenderEnum
{
    Male = 1,
    Female,
}

public enum IdentifierTypeEnum
{
    NationalId = 1,
    Passport,
}

/// <summary>How a portal user is related to the patient.</summary>
public enum RelationshipEnum
{
    Self = 1,
    Parent,
    Guardian,
    Other,
}

/// <summary>
/// What the patient (or guardian) agreed to. The exact legal wording needs review (assumptions L1, L2);
/// this records that consent was given and when.
/// </summary>
public enum ConsentTypeEnum
{
    Treatment = 1,
    DataProcessing,
    Marketing,
}
