namespace Core.Interfaces.Common;

/// <summary>
/// Append-only audit trail (sign-ins, role changes, Platform Admin actions, later medical record access).
/// Never put medical details, passwords, tokens or national IDs in <paramref name="detail"/>.
/// </summary>
public interface IAuditService
{
    Task LogAsync(string action, string? entity = null, long? entityId = null, long? targetTenantId = null,
        string? detail = null, long? userId = null, CancellationToken cancellationToken = default);
}
