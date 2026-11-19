using Core.Entities.Billing;
using Core.Enums.Billing;

namespace Shifa.Tests.Unit.Billing;

/// <summary>Invoice money rules (ADR-011): PaidAmount + CreditedAmount ≤ Total, nothing negative, void only when untouched.</summary>
public class InvoiceTests
{
    private static Invoice New(decimal total)
    {
        var invoice = new Invoice();
        invoice.SetTotals(total);
        return invoice;
    }

    [Fact]
    public void Payments_move_the_status_and_can_never_exceed_the_balance()
    {
        var invoice = New(300);
        Assert.Equal(InvoiceStatusEnum.Open, invoice.Status);

        Assert.True(invoice.TryApplyPayment(100));
        Assert.Equal(InvoiceStatusEnum.PartiallyPaid, invoice.Status);
        Assert.False(invoice.TryApplyPayment(200.01m));
        Assert.False(invoice.TryApplyPayment(0));
        Assert.False(invoice.TryApplyPayment(-5));

        Assert.True(invoice.TryApplyPayment(200));
        Assert.Equal(InvoiceStatusEnum.Paid, invoice.Status);
        Assert.Equal(0, invoice.Balance);
        Assert.False(invoice.TryApplyPayment(0.01m));
    }

    [Fact]
    public void A_refund_reopens_the_balance_and_can_never_exceed_what_was_paid()
    {
        var invoice = New(300);
        invoice.TryApplyPayment(300);

        Assert.False(invoice.TryApplyRefund(300.01m));
        Assert.True(invoice.TryApplyRefund(40));
        Assert.Equal(InvoiceStatusEnum.PartiallyPaid, invoice.Status);
        Assert.Equal(40, invoice.Balance);
    }

    [Fact]
    public void A_credit_note_only_covers_what_is_still_owed()
    {
        var invoice = New(300);
        invoice.TryApplyPayment(260);

        Assert.False(invoice.TryApplyCredit(40.01m));
        Assert.True(invoice.TryApplyCredit(40));
        Assert.Equal(InvoiceStatusEnum.Paid, invoice.Status);
        Assert.Equal(40, invoice.CreditedAmount);
    }

    [Fact]
    public void Void_only_when_nothing_was_paid_or_credited_and_then_no_money_moves()
    {
        var paid = New(100);
        paid.TryApplyPayment(10);
        Assert.False(paid.TryVoid("x", DateTimeOffset.UnixEpoch));

        var credited = New(100);
        credited.TryApplyCredit(10);
        Assert.False(credited.TryVoid("x", DateTimeOffset.UnixEpoch));

        var untouched = New(100);
        Assert.True(untouched.TryVoid("Entered by mistake", DateTimeOffset.UnixEpoch));
        Assert.Equal(InvoiceStatusEnum.Void, untouched.Status);
        Assert.False(untouched.TryApplyPayment(10));
        Assert.False(untouched.TryApplyCredit(10));
        Assert.False(untouched.TryVoid("again", DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void Totals_are_set_once_before_any_money()
    {
        var invoice = New(100);
        invoice.TryApplyPayment(10);
        Assert.Throws<InvalidOperationException>(() => invoice.SetTotals(200));
    }

    [Fact]
    public void Any_sequence_of_operations_keeps_the_invariant()
    {
        var random = new Random(20261119);
        var invoice = New(1000);
        for (var i = 0; i < 2000; i++)
        {
            var amount = Math.Round((decimal)random.NextDouble() * 400 - 50, 2);
            _ = random.Next(3) switch
            {
                0 => invoice.TryApplyPayment(amount),
                1 => invoice.TryApplyRefund(amount),
                _ => invoice.TryApplyCredit(amount),
            };

            Assert.True(invoice.PaidAmount >= 0 && invoice.CreditedAmount >= 0);
            Assert.True(invoice.PaidAmount + invoice.CreditedAmount <= invoice.Total);
            Assert.Equal(invoice.Total - invoice.PaidAmount - invoice.CreditedAmount, invoice.Balance);
        }
    }
}
