using Core.Entities.Identity;
using Core.Enums.Identity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Identity;

/// <summary>
/// Issues one-time password tokens (ADR-014): 256-bit random, stored as SHA-256 only. Issuing a token invalidates the
/// user's earlier unused tokens of the same purpose, so only the newest link works.
/// </summary>
public sealed class PasswordTokens
{
    private readonly IdentityDbContext _context;
    private readonly ITokenService _tokens;
    private readonly TimeProvider _time;
    private readonly string? _staffAppUrl;

    public PasswordTokens(IdentityDbContext context, ITokenService tokens, TimeProvider time, IConfiguration configuration)
    {
        _context = context;
        _tokens = tokens;
        _time = time;
        _staffAppUrl = configuration["App:StaffAppUrl"];
    }

    /// <summary>Adds the token to the context (the caller saves) and returns the raw value for the e-mail.</summary>
    public async Task<string> IssueAsync(long userId, PasswordTokenPurposeEnum purpose, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var earlier = await _context.PasswordTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in earlier)
            token.UsedAt = now;

        var (raw, hash) = _tokens.CreateRefreshToken();
        _context.PasswordTokens.Add(new PasswordToken
        {
            UserId = userId,
            Purpose = purpose,
            TokenHash = hash,
            ExpiresAt = now + (purpose == PasswordTokenPurposeEnum.Reset ? PasswordToken.ResetLifetime : PasswordToken.InvitationLifetime),
        });
        return raw;
    }

    public string Hash(string raw) => _tokens.HashRefreshToken(raw.Trim());

    /// <summary>The link for the e-mail (App:StaffAppUrl/set-password?token=…), or the bare token when no app URL is configured.</summary>
    public string Link(string raw) =>
        string.IsNullOrWhiteSpace(_staffAppUrl) ? raw : $"{_staffAppUrl.TrimEnd('/')}/set-password?token={Uri.EscapeDataString(raw)}";
}
