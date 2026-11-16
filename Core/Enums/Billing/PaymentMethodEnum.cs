namespace Core.Enums.Billing;

public enum PaymentMethodEnum
{
    Cash = 1,

    /// <summary>POS terminal; reference required.</summary>
    Card,

    /// <summary>InstaPay / Vodafone Cash; reference required.</summary>
    Wallet,

    /// <summary>Paid by the patient through the online gateway (Paymob); no cashier shift.</summary>
    Online,
}
