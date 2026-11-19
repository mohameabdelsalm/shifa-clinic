using Core.Common;

namespace Core.Interfaces.Tenancy;

public interface ISubscriptionRepo
{
    /// <summary>The caller's clinic subscription and read-only state (A6).</summary>
    Task<Response> GetMine(CancellationToken cancellationToken);
}
