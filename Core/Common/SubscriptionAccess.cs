using Core.Entities.Tenancy;
using Core.Enums.Tenancy;

namespace Core.Common;

/// <summary>
/// Assumption A6 (ADR-014): when a clinic's subscription ends, staff and patients keep read-only access for 30 days
/// (no changes, no new bookings); after that nobody can sign in until the Platform Admin records a payment or
/// changes the plan. Suspension by the Platform Admin is separate and blocks at once.
/// </summary>
public static class SubscriptionAccess
{
    public static readonly TimeSpan ReadOnlyGrace = TimeSpan.FromDays(30);

    /// <summary>When the latest subscription ended, or null while it runs (an Active subscription without EndsAt never ends).</summary>
    public static DateTimeOffset? EndedAt(TenantSubscription? latest) => latest?.Status switch
    {
        null => null,
        SubscriptionStatusEnum.Trial => latest.TrialEndsAt,
        SubscriptionStatusEnum.Active or SubscriptionStatusEnum.PastDue => latest.EndsAt,
        _ => latest.EndsAt ?? latest.StartsAt,
    };

    public static (bool ReadOnly, bool Blocked) Evaluate(TenantSubscription? latest, DateTimeOffset now)
    {
        var endedAt = EndedAt(latest);
        if (endedAt is null || endedAt > now)
            return (false, false);
        var blocked = now >= endedAt.Value + ReadOnlyGrace;
        return (!blocked, blocked);
    }
}
