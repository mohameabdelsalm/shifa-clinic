using System.Reflection;

namespace Shifa.Core.Common;

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
    public const string BillingRead = "Billing.Read";
    public const string BillingCollect = "Billing.Collect";
    public const string BillingRefund = "Billing.Refund";
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
            AppointmentsRead, AppointmentsCreate, AppointmentsCancel, AppointmentsCheckIn, BillingRead, ReportsRead],
        ["Doctor"] = [PatientsRead, AppointmentsRead, ClinicalRead, ClinicalWrite],
        ["Receptionist"] = [PatientsRead, PatientsCreate, PatientsUpdate, AppointmentsRead, AppointmentsCreate,
            AppointmentsCancel, AppointmentsCheckIn, DoctorsRead, ServicesRead],
        ["Cashier"] = [PatientsRead, AppointmentsRead, BillingRead, BillingCollect],
        ["Nurse"] = [PatientsRead, AppointmentsRead, ClinicalRead],
    };
}
