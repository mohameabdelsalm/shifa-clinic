using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Shifa.Identity.Contracts;

namespace Shifa.Identity.Services;

/// <summary>
/// Short-lived cache of membership access and platform-admin checks (ADR-007).
/// Keys start with the tenant id (t:{tenantId}:...). Changing roles or memberships bumps the tenant's version,
/// which makes every cached entry of that tenant unreachable at once; entries also expire after <see cref="Ttl"/>.
/// </summary>
public sealed class MembershipAccessCache
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<long, int> _tenantVersions = new();

    public MembershipAccessCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public MembershipAccess? GetMembership(long tenantId, long membershipId) =>
        _cache.TryGetValue(MembershipKey(tenantId, membershipId), out MembershipAccess? access) ? access : null;

    public void SetMembership(MembershipAccess access) =>
        _cache.Set(MembershipKey(access.TenantId, access.MembershipId), access, Ttl);

    public bool? GetPlatformAdmin(long userId) =>
        _cache.TryGetValue(PlatformKey(userId), out bool value) ? value : null;

    public void SetPlatformAdmin(long userId, bool isAdmin) => _cache.Set(PlatformKey(userId), isAdmin, Ttl);

    public bool? GetActiveUser(long userId) => _cache.TryGetValue(UserKey(userId), out bool value) ? value : null;

    public void SetActiveUser(long userId, bool isActive) => _cache.Set(UserKey(userId), isActive, Ttl);

    public void InvalidateTenant(long tenantId) => _tenantVersions.AddOrUpdate(tenantId, 1, (_, v) => v + 1);

    private string MembershipKey(long tenantId, long membershipId) =>
        $"t:{tenantId}:v{_tenantVersions.GetValueOrDefault(tenantId)}:membership:{membershipId}";

    private static string PlatformKey(long userId) => $"platform:admin:{userId}";

    private static string UserKey(long userId) => $"user:active:{userId}";
}
