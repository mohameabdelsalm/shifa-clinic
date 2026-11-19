namespace Core.Validations.Clinic;

internal static class ClinicRules
{
    public static bool IsValidTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
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
