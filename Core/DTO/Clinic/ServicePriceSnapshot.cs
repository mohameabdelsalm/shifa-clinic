namespace Core.DTO.Clinic;

/// <summary>The current price of a service at a branch, copied onto invoice items (rule 8: price snapshot).</summary>
public sealed record ServicePriceSnapshot(long ServiceId, string NameEn, string NameAr, decimal Price);
