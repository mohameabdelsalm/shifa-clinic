namespace Core.Enums.Patients;

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
