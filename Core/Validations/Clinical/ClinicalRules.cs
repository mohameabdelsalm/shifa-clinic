using System.Text.RegularExpressions;

namespace Core.Validations.Clinical;

public static partial class ClinicalRules
{
    /// <summary>ICD-10 format (letter, two digits, optional dot and 1-4 characters), e.g. J06.9, E11, S72.001A.</summary>
    [GeneratedRegex(@"^[A-TV-Z][0-9][0-9AB](\.[0-9A-TV-Z]{1,4})?$")]
    private static partial Regex Icd10();

    public static string NormalizeIcd10(string code) => code.Trim().ToUpperInvariant();

    public static bool IsValidIcd10(string? code) => !string.IsNullOrWhiteSpace(code) && Icd10().IsMatch(NormalizeIcd10(code));
}
