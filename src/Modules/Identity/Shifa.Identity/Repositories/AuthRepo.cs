using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.Core.Enums.Common;
using Shifa.Core.Interfaces.Common;
using Shifa.Identity.Data;
using Shifa.Identity.DTO;
using Shifa.Identity.Entities;
using Shifa.Identity.Enums;
using Shifa.Identity.Interfaces;
using Shifa.Identity.Models;
using Shifa.Identity.Services;
using Shifa.Tenancy.Contracts;

namespace Shifa.Identity.Repositories;

/// <summary>
/// Sign-in, refresh-token rotation, tenant switching and sign-out (ADR-007).
/// Every credential or session failure returns the same generic 401 message, so the response never says
/// whether the account, the organization or the membership exists.
/// </summary>
public class AuthRepo : IAuthRepo
{
    // Verified against when the user doesn't exist, so response time doesn't reveal that.
    private static readonly Lazy<string> DummyHash = new(() => new PasswordHasher<User>().HashPassword(new User(), "Dummy-Password-1"));

    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITokenService _tokens;
    private readonly ITenancyApi _tenancy;
    private readonly ITenantScopeFactory _scopes;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IStringLocalizer<LanguageResource> _localizer;
    private readonly TimeProvider _time;

    public AuthRepo(IdentityDbContext context, IPasswordHasher<User> hasher, ITokenService tokens, ITenancyApi tenancy,
        ITenantScopeFactory scopes, IAuditService audit, ICurrentUser currentUser,
        IStringLocalizer<LanguageResource> localizer, TimeProvider time)
    {
        _context = context;
        _hasher = hasher;
        _tokens = tokens;
        _tenancy = tenancy;
        _scopes = scopes;
        _audit = audit;
        _currentUser = currentUser;
        _localizer = localizer;
        _time = time;
    }

    public async Task<Response> Login(LoginModel model, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var identifier = model.Identifier.Trim();
        var email = identifier.ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || u.PhoneNumber == identifier, cancellationToken);

        if (user is null)
        {
            _hasher.VerifyHashedPassword(new User(), DummyHash.Value, model.Password);
            await _audit.LogAsync("Auth.LoginFailed", cancellationToken: cancellationToken);
            throw InvalidCredentials();
        }

        if (user.Status != StatusEnum.Active || user.IsLockedOut(now))
        {
            await _audit.LogAsync("Auth.LoginBlocked", "User", user.Id, userId: user.Id, cancellationToken: cancellationToken);
            throw InvalidCredentials();
        }

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RegisterFailedSignIn(now);
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync("Auth.LoginFailed", "User", user.Id, userId: user.Id, cancellationToken: cancellationToken);
            throw InvalidCredentials();
        }

        user.RegisterSuccessfulSignIn();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = _hasher.HashPassword(user, model.Password);

        var (tenantId, membershipId) = await ResolveTargetAsync(user, model.TenantSlug, InvalidCredentials, cancellationToken);
        var dto = CreateTokens(user, tenantId, membershipId, familyId: null, out _);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Auth.LoginSucceeded", "User", user.Id, targetTenantId: tenantId, userId: user.Id, cancellationToken: cancellationToken);
        return Ok(dto);
    }

    public async Task<Response> Refresh(RefreshTokenModel model, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var hash = _tokens.HashRefreshToken(model.RefreshToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw InvalidToken();

        if (token.RevokedAt is not null)
        {
            // An already-used token came back: it was stolen or replayed. Kill the whole family.
            await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
            await _audit.LogAsync("Auth.RefreshReuseDetected", "RefreshToken", token.Id, userId: token.UserId, cancellationToken: cancellationToken);
            throw InvalidToken();
        }

        if (token.ExpiresAt <= now)
            throw InvalidToken();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
        if (user is null || user.Status != StatusEnum.Active || user.IsLockedOut(now))
            throw InvalidToken();

        if (token.TenantId is null)
        {
            if (!user.IsPlatformAdmin)
                throw InvalidToken();
        }
        else
        {
            var tenant = await _tenancy.GetTenantAsync(token.TenantId.Value, cancellationToken);
            if (tenant is null || !tenant.IsActive || token.MembershipId is null)
                throw InvalidToken();

            var membershipId = token.MembershipId.Value;
            var stillActive = await _scopes.RunAsync(tenant.Id, sp => sp.GetRequiredService<IdentityDbContext>().Memberships
                .AnyAsync(m => m.Id == membershipId && m.UserId == user.Id && m.Status == MembershipStatusEnum.Active, cancellationToken));
            if (!stillActive)
                throw InvalidToken();
        }

        // Claim the token atomically in the database: of two concurrent refreshes with the same token,
        // only one updates the row; the other is treated as reuse.
        var claimed = await _context.RefreshTokens
            .Where(t => t.Id == token.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
        if (claimed == 0)
        {
            await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
            await _audit.LogAsync("Auth.RefreshReuseDetected", "RefreshToken", token.Id, userId: token.UserId, cancellationToken: cancellationToken);
            throw InvalidToken();
        }

        token.RevokedAt = now;
        var dto = CreateTokens(user, token.TenantId, token.MembershipId, token.FamilyId, out var replacement);
        token.ReplacedById = replacement.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(dto);
    }

    public async Task<Response> SwitchTenant(SwitchTenantModel model, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId() ?? throw InvalidToken();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Status == StatusEnum.Active, cancellationToken)
            ?? throw InvalidToken();

        var (tenantId, membershipId) = await ResolveTargetAsync(user, model.TenantSlug, InvalidToken, cancellationToken);
        var dto = CreateTokens(user, tenantId, membershipId, familyId: null, out _);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Auth.TenantSwitched", "User", user.Id, targetTenantId: tenantId, userId: user.Id, cancellationToken: cancellationToken);
        return Ok(dto);
    }

    public async Task<Response> Logout(RefreshTokenModel model, CancellationToken cancellationToken)
    {
        var hash = _tokens.HashRefreshToken(model.RefreshToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is not null)
            await RevokeFamilyAsync(token.FamilyId, _time.GetUtcNow(), cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    /// <summary>
    /// Decides which tenant and membership the new token is for. No slug: only a Platform Admin gets a (tenant-less) platform token.
    /// With a slug: the tenant must be active and the user must hold an active membership in it.
    /// </summary>
    private async Task<(long? TenantId, long? MembershipId)> ResolveTargetAsync(
        User user, string? tenantSlug, Func<BusinessException> fail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            return user.IsPlatformAdmin ? (null, null) : throw fail();

        var tenant = await _tenancy.GetTenantBySlugAsync(tenantSlug.Trim().ToLowerInvariant(), cancellationToken);
        if (tenant is null || !tenant.IsActive)
            throw fail();

        var userId = user.Id;
        var membershipId = await _scopes.RunAsync(tenant.Id, sp => sp.GetRequiredService<IdentityDbContext>().Memberships
            .Where(m => m.UserId == userId && m.Status == MembershipStatusEnum.Active)
            .Select(m => (long?)m.Id)
            .FirstOrDefaultAsync(cancellationToken));

        return membershipId is null ? throw fail() : (tenant.Id, membershipId);
    }

    private TokenDto CreateTokens(User user, long? tenantId, long? membershipId, Guid? familyId, out RefreshToken refreshToken)
    {
        var isPlatformAdmin = tenantId is null && user.IsPlatformAdmin;
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user.Id, user.FullName, tenantId, membershipId, isPlatformAdmin);
        var (raw, hash) = _tokens.CreateRefreshToken();

        refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = tenantId,
            MembershipId = membershipId,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = hash,
            ExpiresAt = _tokens.RefreshTokenExpiry(),
        };
        _context.RefreshTokens.Add(refreshToken);

        return new TokenDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = expiresAt,
            RefreshToken = raw,
            TenantId = tenantId,
            IsPlatformAdmin = isPlatformAdmin,
        };
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tokens = await _context.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.RevokedAt = now;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Response Ok(TokenDto dto) => new(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: dto);

    private BusinessException InvalidCredentials() => new(_localizer["InvalidCredentials"].Value, HttpStatusCode.Unauthorized);

    private BusinessException InvalidToken() => new(_localizer["InvalidToken"].Value, HttpStatusCode.Unauthorized);
}
