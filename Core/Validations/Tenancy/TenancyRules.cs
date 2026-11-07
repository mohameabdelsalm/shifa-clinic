using System.Text.RegularExpressions;
using Core.DTO.Tenancy;

namespace Core.Validations.Tenancy;

internal static partial class TenancyRules
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    public static partial Regex Slug();

    public static bool IsValidEntitlementCode(string code) =>
        code is EntitlementCodes.MaxBranches or EntitlementCodes.MaxDoctors or EntitlementCodes.MaxEmployees
        || (code.StartsWith(EntitlementCodes.ModulePrefix, StringComparison.Ordinal) && code.Length > EntitlementCodes.ModulePrefix.Length && code.Length <= 100);

    public static bool IsValidTimeZone(string? id)
    {
        if (id is null)
            return true;
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
