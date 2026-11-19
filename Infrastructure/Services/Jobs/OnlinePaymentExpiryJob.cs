using Core.Enums.Billing;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.Services.Billing.Paymob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Jobs;

/// <summary>
/// Every 30 minutes: online payment attempts still Pending an hour after their Paymob intention expired become Expired,
/// so staff lists show them as abandoned. Expired is not final: a late callback is still recorded (ADR-013/015).
/// Asking Paymob about each attempt (transaction inquiry) needs merchant API credentials Shifa doesn't store yet.
/// </summary>
public sealed class OnlinePaymentExpiryJob
{
    private readonly BillingDbContext _context;
    private readonly PaymobOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<OnlinePaymentExpiryJob> _logger;

    public OnlinePaymentExpiryJob(BillingDbContext context, IOptions<PaymobOptions> options, TimeProvider time, ILogger<OnlinePaymentExpiryJob> logger)
    {
        _context = context;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 0)]
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var cutoff = now - TimeSpan.FromSeconds(_options.IntentionExpirySeconds) - TimeSpan.FromHours(1);
        var expired = await _context.OnlinePaymentAttempts.IgnoreQueryFilters([ModuleDbContext.TenantFilter])
            .Where(a => a.Status == OnlineAttemptStatusEnum.Pending && a.CreationDate < cutoff && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, OnlineAttemptStatusEnum.Expired)
                .SetProperty(a => a.ModificationDate, now), cancellationToken);

        if (expired > 0)
            _logger.LogInformation("Marked {Count} abandoned online payment attempts as Expired", expired);
        return expired;
    }
}
