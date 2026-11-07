using Core.Common;

namespace Core.Validations.Patients;

internal static class PatientRules
{
    public static bool IsPlausibleBirthDate(DateOnly date) =>
        date >= new DateOnly(1900, 1, 1) && date <= DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool IsValidOptionalPhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) || PhoneNumbers.Normalize(phone) is not null;
}
