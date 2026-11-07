namespace Core.Enums.Patients;

/// <summary>How a portal user is related to the patient.</summary>
public enum RelationshipEnum
{
    Self = 1,
    Parent,
    Guardian,
    Other,
}
