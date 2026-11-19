namespace Core.DTO.Billing;

/// <summary>Money of a period: invoiced and credited (revenue), paid and refunded (collections).</summary>
public sealed record MoneyFlows(IReadOnlyList<MoneyRow> Invoiced, IReadOnlyList<MoneyRow> Credited,
    IReadOnlyList<MoneyRow> Payments, IReadOnlyList<MoneyRow> Refunds);
