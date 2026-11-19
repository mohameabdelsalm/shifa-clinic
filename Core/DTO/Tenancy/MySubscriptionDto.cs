namespace Core.DTO.Tenancy;

/// <summary>The clinic's own subscription state, so the apps can warn before and during read-only mode.</summary>
public class MySubscriptionDto
{
    public string? PlanCode { get; set; }
    public string? PlanName { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>When the subscription ended (null while it runs).</summary>
    public DateTimeOffset? EndedAt { get; set; }
    public bool IsReadOnly { get; set; }

    /// <summary>When sign-in stops if nothing changes (EndedAt + 30 days).</summary>
    public DateTimeOffset? BlockedFrom { get; set; }
}
