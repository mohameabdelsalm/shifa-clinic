namespace Infrastructure.Services.Billing.Paymob;

/// <summary>Paymob answered with an error or not at all. The message never contains keys or billing data.</summary>
public sealed class PaymobException : Exception
{
    public PaymobException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
