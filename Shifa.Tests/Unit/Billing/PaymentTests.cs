using Core.Entities.Billing;

namespace Shifa.Tests.Unit.Billing;

/// <summary>Refund reservation on a payment (ADR-011, ADR-017): never above what's left, a refused one is released.</summary>
public class PaymentTests
{
    [Fact]
    public void Refunds_can_never_exceed_what_was_paid()
    {
        var payment = new Payment { Amount = 250.50m };

        Assert.True(payment.TryRefund(100));
        Assert.Equal(150.50m, payment.Refundable);
        Assert.False(payment.TryRefund(150.51m));
        Assert.False(payment.TryRefund(0));
        Assert.True(payment.TryRefund(150.50m));
        Assert.Equal(0, payment.Refundable);
    }

    [Fact]
    public void A_refused_online_refund_releases_its_reservation()
    {
        var payment = new Payment { Amount = 200 };
        payment.TryRefund(80);          // reserved before calling the gateway

        payment.CancelRefund(80);       // the gateway refused

        Assert.Equal(200, payment.Refundable);
        Assert.Throws<InvalidOperationException>(() => payment.CancelRefund(0.01m));
    }
}
