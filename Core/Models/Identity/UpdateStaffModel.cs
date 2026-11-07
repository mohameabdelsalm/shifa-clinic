namespace Core.Models.Identity;

public class UpdateStaffModel
{
    public long Id { get; set; }
    public List<long> RoleIds { get; set; } = [];
    public bool AllBranches { get; set; }
    public List<long> BranchIds { get; set; } = [];
}
