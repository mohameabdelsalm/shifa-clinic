using System.Text.RegularExpressions;

namespace Shifa.Core.Common;

/// <summary>
/// One format for phone numbers everywhere (clinic registration, OTP login, access links), so "010 1234 5678"
/// typed at the reception matches "+201012345678" typed in the app. Output: "+" and 8–15 digits.
/// Assumption A14: numbers starting with a single 0 are local numbers of the default country (Egypt, 20).
/// </summary>
public static partial class PhoneNumbers
{
    public const string DefaultCountryCode = "20";

    [GeneratedRegex(@"[\s\-().]")]
    private static partial Regex Separators();

    /// <returns>The normalized number, or null when the input isn't a plausible phone number.</returns>
    public static string? Normalize(string? raw, string defaultCountryCode = DefaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var cleaned = Separators().Replace(raw.Trim(), "");
        var hasPlus = cleaned.StartsWith('+');
        var digits = hasPlus ? cleaned[1..] : cleaned;
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
            return null;

        string international;
        if (hasPlus)
            international = digits;
        else if (digits.StartsWith("00", StringComparison.Ordinal))
            international = digits[2..];
        else if (digits.StartsWith('0'))
            international = defaultCountryCode + digits[1..];
        else
            international = digits;

        return international.Length is >= 8 and <= 15 && international[0] != '0' ? "+" + international : null;
    }
}
