namespace Core.Validations.Billing;

public static class MoneyRules
{
    /// <summary>Upper bound for one amount, to catch typos (an extra zero) and keep decimal(18,2) far away.</summary>
    public const decimal MaxAmount = 1_000_000m;

    /// <summary>At most 2 decimals (piasters).</summary>
    public static bool HasTwoDecimalsAtMost(decimal amount) => decimal.Round(amount, 2) == amount;

    public static bool IsValidAmount(decimal amount) => amount > 0 && amount <= MaxAmount && HasTwoDecimalsAtMost(amount);
}
