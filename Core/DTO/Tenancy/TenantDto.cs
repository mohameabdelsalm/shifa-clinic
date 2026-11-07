namespace Core.DTO.Tenancy;

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
