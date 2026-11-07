using Core.Entities.Common;

namespace Core.Entities.Clinic;

/// <summary>
/// The price of a service at one branch. History is kept: changing a price closes the current row
/// (EffectiveTo) and adds a new one, so invoices can always show what was charged and when.
/// </summary>
public class ServicePrice : TenantOwnedEntity
{
    public long ServiceId { get; set; }
    public long BranchId { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
