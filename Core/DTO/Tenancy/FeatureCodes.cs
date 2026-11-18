namespace Core.DTO.Tenancy;

/// <summary>
/// Features that can be switched on or off per plan, and per tenant by the Platform Admin (override), without code
/// changes. Endpoints require them with [RequireFeature(FeatureCodes.X)].
/// </summary>
public static class FeatureCodes
{
    public const string Clinical = "Feature.Clinical";
    public const string Billing = "Feature.Billing";
    public const string PatientPortal = "Feature.PatientPortal";
    public const string Dashboard = "Feature.Dashboard";
    public const string OnlinePayments = "Feature.OnlinePayments";

    /// <summary>Appointment confirmations, reminders and receipts to patients (SMS/e-mail, ADR-015).</summary>
    public const string Notifications = "Feature.Notifications";

    /// <summary>Value when neither the plan nor an override says anything (existing plans keep working).</summary>
    public static readonly IReadOnlyDictionary<string, bool> Defaults = new Dictionary<string, bool>
    {
        [Clinical] = true,
        [Billing] = true,
        [PatientPortal] = true,
        [Dashboard] = true,
        [OnlinePayments] = false,
        [Notifications] = true,
    };

    public static bool IsKnown(string code) => Defaults.ContainsKey(code);
}
