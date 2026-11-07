namespace Core.Models.Tenancy;

public class AddTenantModel
{
    public string Name { get; set; } = "";

    /// <summary>Short unique name: lower case letters, digits and hyphens.</summary>
    public string Slug { get; set; } = "";
    public long PlanId { get; set; }
    public string? DefaultTimeZoneId { get; set; }
    public string AdminFullName { get; set; } = "";
    public string AdminEmail { get; set; } = "";
    public string AdminPassword { get; set; } = "";
}
