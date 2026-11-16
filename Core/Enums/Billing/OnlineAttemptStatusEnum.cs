namespace Core.Enums.Billing;

public enum OnlineAttemptStatusEnum
{
    /// <summary>Intention created; waiting for the gateway callback.</summary>
    Pending = 1,

    /// <summary>Paid and recorded as a Payment.</summary>
    Succeeded,
    Failed,

    /// <summary>Paid at the gateway but the invoice no longer had that balance (paid at the desk meanwhile): needs a refund decision.</summary>
    NotApplied,

    /// <summary>A NotApplied payment that was returned to the patient through the gateway.</summary>
    Refunded,
}
