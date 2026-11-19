using Core.Common;
using Core.Entities.Tenancy;
using Core.Enums.Tenancy;

namespace Shifa.Tests.Unit.Tenancy;

/// <summary>A6 (ADR-014): read-only for 30 days after the subscription ends, then blocked.</summary>
public class SubscriptionAccessTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_running_trial_or_an_open_ended_subscription_gives_full_access()
    {
        Assert.Equal((false, false), SubscriptionAccess.Evaluate(new TenantSubscription { Status = SubscriptionStatusEnum.Trial, TrialEndsAt = Now.AddDays(1) }, Now));
        Assert.Equal((false, false), SubscriptionAccess.Evaluate(new TenantSubscription { Status = SubscriptionStatusEnum.Active }, Now));
        Assert.Equal((false, false), SubscriptionAccess.Evaluate(null, Now));
    }

    [Fact]
    public void Ended_less_than_thirty_days_ago_is_read_only()
    {
        var trial = new TenantSubscription { Status = SubscriptionStatusEnum.Trial, TrialEndsAt = Now.AddDays(-1) };
        Assert.Equal((true, false), SubscriptionAccess.Evaluate(trial, Now));

        var active = new TenantSubscription { Status = SubscriptionStatusEnum.Active, EndsAt = Now.AddDays(-29) };
        Assert.Equal((true, false), SubscriptionAccess.Evaluate(active, Now));
    }

    [Fact]
    public void Thirty_days_after_the_end_sign_in_is_blocked()
    {
        var ended = new TenantSubscription { Status = SubscriptionStatusEnum.Expired, EndsAt = Now - SubscriptionAccess.ReadOnlyGrace };
        Assert.Equal((false, true), SubscriptionAccess.Evaluate(ended, Now));
        Assert.Equal((true, false), SubscriptionAccess.Evaluate(ended, Now.AddTicks(-1)));
    }

    [Fact]
    public void A_cancelled_subscription_without_an_end_date_ended_when_it_started()
    {
        var cancelled = new TenantSubscription { Status = SubscriptionStatusEnum.Cancelled, StartsAt = Now.AddDays(-40) };
        Assert.Equal(Now.AddDays(-40), SubscriptionAccess.EndedAt(cancelled));
        Assert.Equal((false, true), SubscriptionAccess.Evaluate(cancelled, Now));
    }
}
