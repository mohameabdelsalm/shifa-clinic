using System.Text.Json.Serialization;
using Shifa.Core.Common;
using Shifa.Tenancy.Enums;

namespace Shifa.Tenancy.Models;

public class PlanEntitlementModel
{
    /// <summary>MaxBranches, MaxDoctors, MaxEmployees, or Module.&lt;Name&gt;.</summary>
    public string Code { get; set; } = "";
    public bool Enabled { get; set; } = true;

    /// <summary>Null = unlimited (for limits) .</summary>
    public int? LimitValue { get; set; }
}

public class AddPlanModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BillingPeriodEnum BillingPeriod { get; set; } = BillingPeriodEnum.Monthly;
    public int TrialDays { get; set; }
    public List<PlanEntitlementModel> Entitlements { get; set; } = [];
}

public class UpdatePlanModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int TrialDays { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PlanEntitlementModel> Entitlements { get; set; } = [];
}

public class AddTenantModel
{
    public string Name { get; set; } = "";

    /// <summary>Short unique name: lower case letters, digits and hyphens.</summary>
    public string Slug { get; set; } = "";
    public long PlanId { get; set; }
    public string? DefaultTimeZoneId { get; set; }
    public string AdminFullName { get; set; } = "";
    public string AdminEmail { get; set; } = "";
    public string AdminPassword { get; set; } = "";
}

public class ChangePlanModel
{
    public long TenantId { get; set; }
    public long PlanId { get; set; }
}

public class RecordPaymentModel
{
    public long TenantId { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}

public class SearchTenantsModel : PaginationParameters
{
}
