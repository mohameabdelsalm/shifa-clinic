namespace Core.Interfaces.Common;

/// <summary>
/// Stores files by a server-built relative path (tenants/{tenantId}/...). Local disk in development; a blob store later
/// (ADR-010). Never pass a client-supplied name as the path.
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
