using System.Net;
using Core;
using Core.Common;
using Core.DTO.Identity;
using Core.DTO.Tenancy;
using Core.Entities.Identity;
using Core.Enums.Common;
using Core.Enums.Identity;
using Core.Interfaces.Common;
using Core.Interfaces.Identity;
using Core.Interfaces.Patients;
using Core.Interfaces.Tenancy;
using Core.Models.Identity;
using Infrastructure.Data;
using Infrastructure.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Identity;

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
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;
    private readonly PasswordTokens _passwordTokens;
    private readonly IAccessContext _access;

    public AuthRepo(IdentityDbContext context, IPasswordHasher<User> hasher, ITokenService tokens, ITenancyApi tenancy,
        ITenantScopeFactory scopes, IAuditService audit, ICurrentUser currentUser,
        IStringLocalizer<LanguageResource> localizer, TimeProvider time, ISmsSender sms, IEmailSender email, PasswordTokens passwordTokens,
        IAccessContext access)
    {
        _sms = sms;
        _email = email;
        _passwordTokens = passwordTokens;
        _access = access;
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
        var phone = PhoneNumbers.Normalize(identifier);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || (phone != null && u.PhoneNumber == phone), cancellationToken);

        // Phone-only (portal) accounts have no usable password: they sign in with an OTP only.

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

        if (token.IsPortal)
        {
            // A portal session needs an active clinic and a phone-verified user; it has no membership.
            var portalTenant = token.TenantId is null ? null : await _tenancy.GetTenantAsync(token.TenantId.Value, cancellationToken);
            if (portalTenant is not { IsActive: true } || user.PhoneNumber is null)
                throw InvalidToken();
        }
        else if (token.TenantId is null)
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
        var dto = CreateTokens(user, token.TenantId, token.MembershipId, token.FamilyId, out var replacement, token.IsPortal);
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

    /// <summary>
    /// Patient portal sign-in, step 1. The answer is always the same "if registered, a code was sent", so the endpoint
    /// can't be used to find out which phone numbers a clinic knows. A code is only sent (and an SMS only paid for)
    /// when the number is linked to a patient of that clinic, and at most 3 times per 10 minutes.
    /// </summary>
    public async Task<Response> RequestPortalOtp(RequestOtpModel model, CancellationToken cancellationToken)
    {
        var generic = new Response(_localizer["OtpRequested"].Value, HttpStatusCode.OK);
        var phone = PhoneNumbers.Normalize(model.PhoneNumber)!;
        var now = _time.GetUtcNow();

        var tenant = await _tenancy.GetTenantBySlugAsync(model.TenantSlug.Trim().ToLowerInvariant(), cancellationToken);
        // A clinic without the patient portal feature answers like an unknown clinic (no enumeration, no SMS).
        if (tenant is null || !tenant.IsActive || !await _tenancy.IsFeatureEnabledAsync(tenant.Id, FeatureCodes.PatientPortal, cancellationToken))
            return generic;

        var known = await _scopes.RunAsync(tenant.Id, sp => sp.GetRequiredService<IPatientsApi>().HasPortalAccessAsync(phone, cancellationToken));
        if (!known)
            return generic;

        var since = now.AddMinutes(-10);
        var recent = await _context.OtpChallenges.CountAsync(
            c => c.TenantId == tenant.Id && c.PhoneNumber == phone && c.CreationDate > since, cancellationToken);
        if (recent >= 3)
            return generic;

        var code = _tokens.CreateOtpCode();
        _context.OtpChallenges.Add(new OtpChallenge
        {
            TenantId = tenant.Id,
            PhoneNumber = phone,
            CodeHash = _tokens.HashOtp(tenant.Id, phone, code),
            ExpiresAt = now + OtpChallenge.Lifetime,
        });
        await _context.SaveChangesAsync(cancellationToken);

        await _sms.SendAsync(phone, string.Format(_localizer["OtpMessage"].Value, code), cancellationToken);
        await _audit.LogAsync("Portal.OtpRequested", targetTenantId: tenant.Id, cancellationToken: cancellationToken);
        return generic;
    }

    /// <summary>
    /// Patient portal sign-in, step 2. Wrong code, expired code, unknown clinic or unknown number all give the same 401.
    /// A code works once and after 5 wrong tries it is dead. A successful sign-in creates (or reuses) a phone-only user:
    /// the portal token carries the verified phone number, and the patients it may open come from the clinic's access links.
    /// </summary>
    public async Task<Response> VerifyPortalOtp(VerifyOtpModel model, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var phone = PhoneNumbers.Normalize(model.PhoneNumber)!;

        var tenant = await _tenancy.GetTenantBySlugAsync(model.TenantSlug.Trim().ToLowerInvariant(), cancellationToken);
        if (tenant is null || !tenant.IsActive || !await _tenancy.IsFeatureEnabledAsync(tenant.Id, FeatureCodes.PatientPortal, cancellationToken))
            throw InvalidOtp();

        var challenge = await _context.OtpChallenges
            .Where(c => c.TenantId == tenant.Id && c.PhoneNumber == phone && c.ConsumedAt == null && c.ExpiresAt > now && c.Attempts < OtpChallenge.MaxAttempts)
            .OrderByDescending(c => c.CreationDate)
            .FirstOrDefaultAsync(cancellationToken) ?? throw InvalidOtp();

        challenge.Attempts++;
        var expected = _tokens.HashOtp(tenant.Id, phone, model.Code);
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(expected), System.Text.Encoding.ASCII.GetBytes(challenge.CodeHash)))
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync("Portal.OtpFailed", targetTenantId: tenant.Id, cancellationToken: cancellationToken);
            throw InvalidOtp();
        }

        // Claim the code atomically: of two concurrent verifications only one can use it.
        challenge.ConsumedAt = now;
        var claimed = await _context.OtpChallenges
            .Where(c => c.Id == challenge.Id && c.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ConsumedAt, now), cancellationToken);
        if (claimed == 0)
            throw InvalidOtp();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);
        if (user is null)
        {
            user = new User { FullName = phone, PhoneNumber = phone };
            user.PasswordHash = _hasher.HashPassword(user, Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            _context.Users.Add(user);
        }
        else if (user.Status != StatusEnum.Active)
        {
            throw InvalidOtp();
        }

        var dto = CreateTokens(user, tenant.Id, membershipId: null, familyId: null, out _, isPortal: true);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Portal.LoginSucceeded", "User", user.Id, targetTenantId: tenant.Id, userId: user.Id, cancellationToken: cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// Always the same answer, so the endpoint can't tell which e-mails have accounts. For an active account with an
    /// e-mail, at most 3 reset links per hour are sent; each new link replaces the previous one.
    /// </summary>
    public async Task<Response> ForgotPassword(ForgotPasswordModel model, CancellationToken cancellationToken)
    {
        var generic = new Response(_localizer["PasswordResetRequested"].Value, HttpStatusCode.OK);
        var email = model.Email.Trim().ToLowerInvariant();
        var now = _time.GetUtcNow();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status == StatusEnum.Active, cancellationToken);
        if (user is null)
            return generic;

        var since = now.AddHours(-1);
        var recent = await _context.PasswordTokens.CountAsync(
            t => t.UserId == user.Id && t.Purpose == PasswordTokenPurposeEnum.Reset && t.CreationDate > since, cancellationToken);
        if (recent >= 3)
            return generic;

        var raw = await _passwordTokens.IssueAsync(user.Id, PasswordTokenPurposeEnum.Reset, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _email.SendAsync(email, _localizer["PasswordResetEmailSubject"].Value,
            string.Format(_localizer["PasswordResetEmailBody"].Value, user.FullName, _passwordTokens.Link(raw)), cancellationToken);
        await _audit.LogAsync("Auth.PasswordResetRequested", "User", user.Id, userId: user.Id, cancellationToken: cancellationToken);
        return generic;
    }

    /// <summary>
    /// Sets the password with a reset or invitation token. The token is claimed atomically (works once); every other
    /// unused token and every session of the user is revoked, and a lockout is cleared (the e-mail proved ownership).
    /// </summary>
    public async Task<Response> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var hash = _passwordTokens.Hash(model.Token);
        var token = await _context.PasswordTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null || token.UsedAt is not null || token.ExpiresAt <= now)
            throw InvalidPasswordToken();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == token.UserId && u.Status == StatusEnum.Active, cancellationToken)
            ?? throw InvalidPasswordToken();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimed = await _context.PasswordTokens
            .Where(t => t.Id == token.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), cancellationToken);
        if (claimed == 0)
            throw InvalidPasswordToken();

        user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
        user.InvitationPending = false;
        user.RegisterSuccessfulSignIn();
        await _context.SaveChangesAsync(cancellationToken);
        await SignOutEverywhereAsync(user.Id, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var action = token.Purpose == PasswordTokenPurposeEnum.Invitation ? "Auth.InvitationAccepted" : "Auth.PasswordReset";
        await _audit.LogAsync(action, "User", user.Id, userId: user.Id, cancellationToken: CancellationToken.None);
        return new Response(_localizer["PasswordChanged"].Value, HttpStatusCode.OK);
    }

    /// <summary>Staff and Platform Admins only (portal accounts have no password). A wrong current password counts toward lockout.</summary>
    public async Task<Response> ChangePassword(ChangePasswordModel model, CancellationToken cancellationToken)
    {
        if (_access.IsPortalUser)
            throw new BusinessException(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);

        var now = _time.GetUtcNow();
        var userId = _currentUser.GetUserId() ?? throw InvalidToken();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Status == StatusEnum.Active, cancellationToken)
            ?? throw InvalidToken();
        if (user.IsLockedOut(now))
            throw InvalidToken();

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            user.RegisterFailedSignIn(now);
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync("Auth.PasswordChangeFailed", "User", user.Id, userId: user.Id, cancellationToken: cancellationToken);
            throw new BusinessException(_localizer["CurrentPasswordWrong"].Value, HttpStatusCode.BadRequest);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
        user.RegisterSuccessfulSignIn();
        await _context.SaveChangesAsync(cancellationToken);
        await SignOutEverywhereAsync(user.Id, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _audit.LogAsync("Auth.PasswordChanged", "User", user.Id, userId: user.Id, cancellationToken: CancellationToken.None);
        return new Response(_localizer["PasswordChanged"].Value, HttpStatusCode.OK);
    }

    /// <summary>Revokes every refresh token and unused password token of the user (access tokens expire on their own, minutes later).</summary>
    private async Task SignOutEverywhereAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await _context.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
        await _context.PasswordTokens.Where(t => t.UserId == userId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), cancellationToken);
    }

    private BusinessException InvalidPasswordToken() => new(_localizer["InvalidPasswordToken"].Value, HttpStatusCode.BadRequest);

    private TokenDto CreateTokens(User user, long? tenantId, long? membershipId, Guid? familyId, out RefreshToken refreshToken, bool isPortal = false)
    {
        var isPlatformAdmin = !isPortal && tenantId is null && user.IsPlatformAdmin;
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(
            user.Id, user.FullName, tenantId, membershipId, isPlatformAdmin, isPortal ? user.PhoneNumber : null);
        var (raw, hash) = _tokens.CreateRefreshToken();

        refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = tenantId,
            MembershipId = membershipId,
            IsPortal = isPortal,
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

    private BusinessException InvalidOtp() => new(_localizer["InvalidOtp"].Value, HttpStatusCode.Unauthorized);
}
