using Shifa.Core.Common;

namespace Shifa.Identity.Models;

public class AddStaffModel
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>Required for a new person. Ignored when the email already belongs to a user: that account is linked to this organization.</summary>
    public string? Password { get; set; }

    public List<long> RoleIds { get; set; } = [];
    public bool AllBranches { get; set; }
    public List<long> BranchIds { get; set; } = [];
}

public class UpdateStaffModel
{
    public long Id { get; set; }
    public List<long> RoleIds { get; set; } = [];
    public bool AllBranches { get; set; }
    public List<long> BranchIds { get; set; } = [];
}

public class SearchStaffModel : PaginationParameters
{
}
