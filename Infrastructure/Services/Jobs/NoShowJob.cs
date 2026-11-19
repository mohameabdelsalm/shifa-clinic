using Core.Enums.Scheduling;
using Core.Interfaces.Common;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.Services.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Jobs;

/// <summary>Every 5 minutes: confirmed appointments 30+ minutes past their start without check-in become NoShow (A10).</summary>
public sealed class NoShowJob
{
    private const int BatchSize = 500;

    private readonly SchedulingDbContext _context;
    private readonly ITenantScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger<NoShowJob> _logger;

    public NoShowJob(SchedulingDbContext context, ITenantScopeFactory scopes, TimeProvider time, ILogger<NoShowJob> logger)
    {
        _context = context;
        _scopes = scopes;
        _time = time;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 0)]
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = _time.GetUtcNow() - SchedulingRules.NoShowAfter;
        // Across clinics (the job has no tenant): only the tenant filter is ignored; each clinic is then handled in its own scope.
        var due = await _context.Appointments.IgnoreQueryFilters([ModuleDbContext.TenantFilter]).AsNoTracking()
            .Where(a => a.Status == AppointmentStatusEnum.Confirmed && a.StartAt <= cutoff)
            .OrderBy(a => a.StartAt)
            .Take(BatchSize)
            .Select(a => new { a.Id, a.TenantId })
            .ToListAsync(cancellationToken);

        var marked = 0;
        foreach (var tenant in due.GroupBy(a => a.TenantId))
        {
            var ids = tenant.Select(a => a.Id).ToList();
            marked += await _scopes.RunAsync(tenant.Key, sp => sp.GetRequiredService<NoShowMarker>().MarkAsync(ids, cancellationToken));
        }

        if (marked > 0)
            _logger.LogInformation("No-show job marked {Count} appointments", marked);
        return marked;
    }
}
