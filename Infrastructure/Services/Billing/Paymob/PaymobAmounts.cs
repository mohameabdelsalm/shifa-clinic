namespace Infrastructure.Services.Billing.Paymob;

/// <summary>Paymob amounts are integers in the smallest unit (piasters for EGP).</summary>
public static class PaymobAmounts
{
    public static long ToCents(decimal amount) => (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
}
