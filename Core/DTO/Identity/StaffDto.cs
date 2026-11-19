namespace Core.DTO.Identity;

public class StaffDto
{
    /// <summary>The membership id (the id used by the staff endpoints).</summary>
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string Status { get; set; } = "";
    public bool AllBranches { get; set; }
    public List<long> BranchIds { get; set; } = [];
    public List<long> RoleIds { get; set; } = [];

    /// <summary>Invited but hasn't set a password yet.</summary>
    public bool InvitationPending { get; set; }
}
