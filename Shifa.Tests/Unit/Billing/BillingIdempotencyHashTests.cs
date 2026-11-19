using System.Globalization;
using Infrastructure.Services.Billing;

namespace Shifa.Tests.Unit.Billing;

/// <summary>A retry must hash the same whatever the request culture or how the amount was written (ADR-017).</summary>
public class BillingIdempotencyHashTests
{
    [Fact]
    public void Same_request_hashes_the_same_in_every_culture_and_amount_format()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var english = BillingIdempotency.Hash(42L, 100m, "Cash", "POS-1");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
            Assert.Equal(english, BillingIdempotency.Hash(42L, 100.00m, "Cash", "POS-1"));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");   // comma decimal separator
            Assert.Equal(english, BillingIdempotency.Hash(42L, 100.0m, "Cash", "POS-1"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_different_amount_or_reference_is_a_different_request()
    {
        var hash = BillingIdempotency.Hash(42L, 100m, "Cash", null);
        Assert.NotEqual(hash, BillingIdempotency.Hash(42L, 100.01m, "Cash", null));
        Assert.NotEqual(hash, BillingIdempotency.Hash(43L, 100m, "Cash", null));
        Assert.NotEqual(hash, BillingIdempotency.Hash(42L, 100m, "Card", null));
    }
}
