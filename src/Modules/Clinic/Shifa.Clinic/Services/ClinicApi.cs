using Microsoft.EntityFrameworkCore;
using Shifa.Clinic.Contracts;
using Shifa.Clinic.Data;
using Shifa.Core.Enums.Common;

namespace Shifa.Clinic.Services;

/// <summary>The public face of the Clinic module (Clinic.Contracts). Runs in the caller's tenant context.</summary>
public sealed class ClinicApi : IClinicApi
{
    private readonly ClinicDbContext _context;

    public ClinicApi(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<long>> GetExistingBranchIdsAsync(IReadOnlyCollection<long> branchIds, CancellationToken cancellationToken = default) =>
        await _context.Branches.AsNoTracking()
            .Where(b => branchIds.Contains(b.Id) && b.Status == StatusEnum.Active)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
}
