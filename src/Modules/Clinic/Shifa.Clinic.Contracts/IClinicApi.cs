namespace Shifa.Clinic.Contracts;

public interface IClinicApi
{
    /// <summary>
    /// The ids of <paramref name="branchIds"/> that exist and are active in the current tenant.
    /// Other modules use this to validate branch ids they store (no cross-schema foreign keys).
    /// </summary>
    Task<IReadOnlyList<long>> GetExistingBranchIdsAsync(IReadOnlyCollection<long> branchIds, CancellationToken cancellationToken = default);
}
