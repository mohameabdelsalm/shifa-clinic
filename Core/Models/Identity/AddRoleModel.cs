namespace Core.Models.Identity;

public class AddRoleModel
{
    public string Name { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
}
