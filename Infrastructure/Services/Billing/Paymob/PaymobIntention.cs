namespace Infrastructure.Services.Billing.Paymob;

/// <param name="OrderId">Paymob's order id for the intention (intention_order_id); callbacks carry it as the signed order.id.</param>
public sealed record PaymobIntention(string Id, string ClientSecret, string? OrderId);
