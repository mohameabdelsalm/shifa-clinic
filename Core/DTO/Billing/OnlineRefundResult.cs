using Core.Enums.Billing;

namespace Core.DTO.Billing;

/// <summary>What the gateway said about a refund, and its refund transaction id when it gave one.</summary>
public sealed record OnlineRefundResult(OnlineRefundOutcomeEnum Outcome, string? Reference);
