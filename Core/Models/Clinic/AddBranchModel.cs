namespace Core.Models.Clinic;

public class AddBranchModel
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? TimeZoneId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
