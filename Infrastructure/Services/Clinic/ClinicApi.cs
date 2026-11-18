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

    public async Task<IReadOnlyDictionary<long, string>> GetBranchTimeZonesAsync(IReadOnlyCollection<long>? branchIds, CancellationToken cancellationToken = default)
    {
        var ids = branchIds?.ToList();
        return await _context.Branches.AsNoTracking()
            .Where(b => ids == null || ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.TimeZoneId, cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorCapacityRow>> GetDoctorCapacityAsync(DateOnly fromLocalDate, DateOnly toLocalDate,
        IReadOnlyCollection<long>? branchIds, long? doctorId, CancellationToken cancellationToken = default)
    {
        var ids = branchIds?.ToList();
        var schedules = await (from s in _context.DoctorSchedules.AsNoTracking()
                               join db in _context.DoctorBranches.AsNoTracking() on s.DoctorBranchId equals db.Id
                               join b in _context.Branches.AsNoTracking() on db.BranchId equals b.Id
                               where (ids == null || ids.Contains(db.BranchId)) && (doctorId == null || db.DoctorId == doctorId)
                               select new { db.DoctorId, db.BranchId, b.TimeZoneId, s.DayOfWeek, s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);
        if (schedules.Count == 0)
            return [];

        // Time off is stored in UTC; a day before/after covers every time zone offset.
        var windowFrom = new DateTimeOffset(fromLocalDate.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var windowTo = new DateTimeOffset(toLocalDate.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var doctorIds = schedules.Select(s => s.DoctorId).Distinct().ToList();
        var timeOff = (await _context.DoctorTimeOffs.AsNoTracking()
                .Where(t => doctorIds.Contains(t.DoctorId) && t.FromAt < windowTo && windowFrom < t.ToAt)
                .Select(t => new { t.DoctorId, t.FromAt, t.ToAt })
                .ToListAsync(cancellationToken))
            .ToLookup(t => t.DoctorId);

        var minutes = new Dictionary<(long DoctorId, long BranchId), double>();
        foreach (var schedule in schedules)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
            for (var day = fromLocalDate; day <= toLocalDate; day = day.AddDays(1))
            {
                if (day.DayOfWeek != schedule.DayOfWeek)
                    continue;

                var start = ToUtc(day, schedule.StartTime, zone);
                var end = ToUtc(day, schedule.EndTime, zone);
                var worked = (end - start).TotalMinutes;
                foreach (var off in timeOff[schedule.DoctorId])
                {
                    var overlapStart = off.FromAt > start ? off.FromAt : start;
                    var overlapEnd = off.ToAt < end ? off.ToAt : end;
                    if (overlapEnd > overlapStart)
                        worked -= (overlapEnd - overlapStart).TotalMinutes;
                }

                var key = (schedule.DoctorId, schedule.BranchId);
                minutes[key] = minutes.GetValueOrDefault(key) + Math.Max(0, worked);
            }
        }

        return minutes.Select(m => new DoctorCapacityRow(m.Key.DoctorId, m.Key.BranchId, (int)Math.Round(m.Value))).ToList();
    }

    private static DateTimeOffset ToUtc(DateOnly day, TimeOnly time, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }

    public async Task<IReadOnlyList<TimeRange>> GetDoctorTimeOffAsync(long doctorId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        await _context.DoctorTimeOffs.AsNoTracking()
            .Where(t => t.DoctorId == doctorId && t.FromAt < toUtc && fromUtc < t.ToAt)
            .Select(t => new TimeRange(t.FromAt, t.ToAt))
            .ToListAsync(cancellationToken);
}
