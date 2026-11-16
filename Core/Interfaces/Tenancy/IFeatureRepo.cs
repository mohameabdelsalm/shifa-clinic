using Core.Common;

namespace Core.Interfaces.Tenancy;

public interface IFeatureRepo
{
    /// <summary>The features of the caller's tenant, so the UI can hide what the clinic doesn't use.</summary>
    Task<Response> GetMine(CancellationToken cancellationToken);
}
