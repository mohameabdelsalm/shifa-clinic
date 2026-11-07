namespace Core.Interfaces.Clinical;

/// <summary>What other modules may ask Clinical.</summary>
public interface IClinicalApi
{
    /// <summary>True while the doctor has a visit that isn't completed yet.</summary>
    Task<bool> HasOpenVisitsAsync(long doctorId, CancellationToken cancellationToken = default);
}
