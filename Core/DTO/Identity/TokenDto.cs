namespace Core.DTO.Identity;

public class TokenDto
{
    public string AccessToken { get; set; } = "";
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = "";
    public long? TenantId { get; set; }
    public bool IsPlatformAdmin { get; set; }
}
