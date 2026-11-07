using System.Text.Json.Serialization;
using Core.Enums.Tenancy;

namespace Core.Models.Tenancy;

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
