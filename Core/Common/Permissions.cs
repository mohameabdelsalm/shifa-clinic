using System.Reflection;

namespace Core.Common;

/// <summary>
/// The permission catalog (ADR-007). Permissions are defined in code; roles (per tenant) group them.
/// Endpoints require one with [Permission(Permissions.X)].
/// </summary>
public static class Permissions
{
    public const string RolesRead = "Roles.Read";
    public const string RolesManage = "Roles.Manage";
    public const string StaffRead = "Staff.Read";
    public const string StaffManage = "Staff.Manage";
    public const string BranchesRead = "Branches.Read";
    public const string BranchesManage = "Branches.Manage";
    public const string DoctorsRead = "Doctors.Read";
    public const string DoctorsManage = "Doctors.Manage";
    public const string ServicesRead = "Services.Read";
    public const string ServicesManage = "Services.Manage";
    public const string PatientsRead = "Patients.Read";
    public const string PatientsCreate = "Patients.Create";
    public const string PatientsUpdate = "Patients.Update";
    public const string AppointmentsRead = "Appointments.Read";
    public const string AppointmentsCreate = "Appointments.Create";
    public const string AppointmentsCancel = "Appointments.Cancel";
    public const string AppointmentsCheckIn = "Appointments.CheckIn";
    public const string ClinicalRead = "Clinical.Read";
    public const string ClinicalWrite = "Clinical.Write";

    /// <summary>Record vital signs on an open visit in one's branches (doctors and nurses, ADR-010).</summary>
    public const string ClinicalVitals = "Clinical.Vitals";
    public const string BillingRead = "Billing.Read";
    public const string BillingCollect = "Billing.Collect";
    public const string BillingRefund = "Billing.Refund";

    /// <summary>Manage the clinic's online payment gateway (Paymob keys).</summary>
    public const string BillingSettings = "Billing.Settings";

    /// <summary>See which messages were sent to patients.</summary>
    public const string NotificationsRead = "Notifications.Read";

    /// <summary>Edit the clinic's message templates.</summary>
    public const string NotificationsManage = "Notifications.Manage";
    public const string ReportsRead = "Reports.Read";

    public static readonly IReadOnlyList<string> All = typeof(Permissions)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();

    public const string TenantAdminRole = "TenantAdmin";

    /// <summary>Roles created for every new tenant (assumption A7). TenantAdmin always has every permission.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultRoles = new Dictionary<string, IReadOnlyList<string>>
    {
        [TenantAdminRole] = All,
        ["BranchManager"] = [StaffRead, BranchesRead, DoctorsRead, ServicesRead, PatientsRead, PatientsCreate, PatientsUpdate,
            AppointmentsRead, AppointmentsCreate, AppointmentsCancel, AppointmentsCheckIn, BillingRead, BillingRefund, ReportsRead, NotificationsRead],
        ["Doctor"] = [PatientsRead, AppointmentsRead, ClinicalRead, ClinicalWrite, ClinicalVitals],
        ["Receptionist"] = [PatientsRead, PatientsCreate, PatientsUpdate, AppointmentsRead, AppointmentsCreate, BillingRead, BillingCollect,
            AppointmentsCancel, AppointmentsCheckIn, DoctorsRead, ServicesRead, NotificationsRead],
        ["Cashier"] = [PatientsRead, AppointmentsRead, BillingRead, BillingCollect],
        ["Nurse"] = [PatientsRead, AppointmentsRead, ClinicalRead, ClinicalVitals],
    };
}
