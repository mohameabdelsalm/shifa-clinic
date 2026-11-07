namespace Shifa.Tenancy.DTO;

public class PlanEntitlementDto
{
    public string Code { get; set; } = "";
    public bool Enabled { get; set; }
    public int? LimitValue { get; set; }
}

public class PlanDto
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string BillingPeriod { get; set; } = "";
    public int TrialDays { get; set; }
    public bool IsActive { get; set; }
    public List<PlanEntitlementDto> Entitlements { get; set; } = [];
}

public class TenantDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Status { get; set; } = "";
    public string DefaultTimeZoneId { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public string? PlanCode { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset? SubscriptionEndsAt { get; set; }
}
