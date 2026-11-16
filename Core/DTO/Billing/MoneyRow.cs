using Core.Enums.Billing;

namespace Core.DTO.Billing;

/// <summary>An amount on one UTC day at one branch (by payment method where it applies), for statistics.</summary>
public sealed record MoneyRow(DateOnly Date, long BranchId, PaymentMethodEnum? Method, decimal Amount);
