namespace Shifa.Identity.Models;

public class AddRoleModel
{
    public string Name { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
}

public class UpdateRoleModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
}
