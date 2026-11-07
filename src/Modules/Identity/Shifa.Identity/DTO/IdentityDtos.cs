namespace Shifa.Identity.DTO;

public class TokenDto
{
    public string AccessToken { get; set; } = "";
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = "";
    public long? TenantId { get; set; }
    public bool IsPlatformAdmin { get; set; }
}

public class RoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsSystemDefault { get; set; }
    public List<string> Permissions { get; set; } = [];
}

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
}
