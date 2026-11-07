using Core.Enums.Clinical;
using Core.Interfaces.Clinical;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Clinical;

public sealed class ClinicalApi : IClinicalApi
{
    private readonly ClinicalDbContext _context;

    public ClinicalApi(ClinicalDbContext context)
    {
        _context = context;
    }

    public Task<bool> HasOpenVisitsAsync(long doctorId, CancellationToken cancellationToken = default) =>
        _context.Visits.AnyAsync(v => v.DoctorId == doctorId && v.Status == VisitStatusEnum.Open, cancellationToken);
}
