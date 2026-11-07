namespace Core.Models.Identity;

public class UpdateRoleModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
}
