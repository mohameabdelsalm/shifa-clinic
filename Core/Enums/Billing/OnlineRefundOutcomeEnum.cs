namespace Core.Enums.Billing;

public enum OnlineRefundOutcomeEnum
{
    Succeeded = 1,

    /// <summary>The gateway definitely did not refund (refused, not configured).</summary>
    Refused,

    /// <summary>No definite answer (timeout, no response, "pending"): the money may still be returned.</summary>
    Unknown,
}
