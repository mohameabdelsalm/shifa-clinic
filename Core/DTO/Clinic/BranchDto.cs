namespace Core.DTO.Clinic;

public class BranchDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "";
}
