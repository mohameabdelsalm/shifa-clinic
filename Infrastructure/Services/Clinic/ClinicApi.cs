using Core.DTO.Clinic;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Clinic;

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

    public async Task<BookingInputs?> GetBookingInputsAsync(long branchId, long doctorId, long serviceId, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches.AsNoTracking()
            .Where(b => b.Id == branchId && b.Status == StatusEnum.Active)
            .Select(b => new { b.Id, b.TimeZoneId })
            .FirstOrDefaultAsync(cancellationToken);
        if (branch is null)
            return null;

        var doctorActive = await _context.Doctors.AnyAsync(d => d.Id == doctorId && d.Status == StatusEnum.Active, cancellationToken);
        var doctorBranchId = await _context.DoctorBranches
            .Where(b => b.DoctorId == doctorId && b.BranchId == branchId)
            .Select(b => (long?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!doctorActive || doctorBranchId is null)
            return null;

        var service = await _context.Services.AsNoTracking()
            .Where(s => s.Id == serviceId && s.Status == StatusEnum.Active)
            .Select(s => new { s.Id, s.SpecialtyId, s.DurationMinutes })
            .FirstOrDefaultAsync(cancellationToken);
        if (service is null ||
            !await _context.DoctorSpecialties.AnyAsync(s => s.DoctorId == doctorId && s.SpecialtyId == service.SpecialtyId, cancellationToken))
            return null;

        // The visit is invoiced at check-in (ADR-011), so the service must have a price at this branch.
        if (!await _context.ServicePrices.AnyAsync(p => p.ServiceId == serviceId && p.BranchId == branchId && p.EffectiveTo == null, cancellationToken))
            return null;

        var schedule = await _context.DoctorSchedules.AsNoTracking()
            .Where(s => s.DoctorBranchId == doctorBranchId)
            .Select(s => new ScheduleWindow(s.DayOfWeek, s.StartTime, s.EndTime, s.SlotMinutes))
            .ToListAsync(cancellationToken);

        return new BookingInputs(branch.Id, branch.TimeZoneId, doctorId, service.Id, service.DurationMinutes, schedule);
    }

    public Task<string?> GetBranchTimeZoneAsync(long branchId, CancellationToken cancellationToken = default) =>
        _context.Branches.Where(b => b.Id == branchId).Select(b => (string?)b.TimeZoneId).FirstOrDefaultAsync(cancellationToken);

    public async Task<ServicePriceSnapshot?> GetCurrentPriceAsync(long serviceId, long branchId, CancellationToken cancellationToken = default) =>
        await (from p in _context.ServicePrices.AsNoTracking()
               join s in _context.Services.AsNoTracking() on p.ServiceId equals s.Id
               where p.ServiceId == serviceId && p.BranchId == branchId && p.EffectiveTo == null && s.Status == StatusEnum.Active
               select new ServicePriceSnapshot(s.Id, s.NameEn, s.NameAr, p.Price))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<long, string>> GetBranchNamesAsync(IReadOnlyCollection<long> branchIds, CancellationToken cancellationToken = default)
    {
        var ids = branchIds.ToList();
        return await _context.Branches.AsNoTracking().Where(b => ids.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<long, string>> GetDoctorNamesAsync(IReadOnlyCollection<long> doctorIds, CancellationToken cancellationToken = default)
    {
        var ids = doctorIds.ToList();
        return await _context.Doctors.AsNoTracking().Where(d => ids.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.FullNameEn, cancellationToken);
    }

    public Task<long?> GetDoctorIdForMembershipAsync(long membershipId, CancellationToken cancellationToken = default) =>
        _context.Doctors.Where(d => d.MembershipId == membershipId && d.Status == StatusEnum.Active)
            .Select(d => (long?)d.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TimeRange>> GetDoctorTimeOffAsync(long doctorId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        await _context.DoctorTimeOffs.AsNoTracking()
            .Where(t => t.DoctorId == doctorId && t.FromAt < toUtc && fromUtc < t.ToAt)
            .Select(t => new TimeRange(t.FromAt, t.ToAt))
            .ToListAsync(cancellationToken);
}
