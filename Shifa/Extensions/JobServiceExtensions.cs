using Hangfire;
using Hangfire.SqlServer;
using Infrastructure.Data;
using Infrastructure.Services.Jobs;

namespace Shifa.Extensions;

/// <summary>
/// Hangfire (ADR-015): SQL Server storage in the "hangfire" schema of the same database, server in the API process.
/// Jobs:Enabled=false turns the server and the recurring jobs off on an instance.
/// </summary>
public static class JobServiceExtensions
{
    private static readonly Dictionary<string, (string Cron, Func<IServiceProvider, CancellationToken, Task<int>> Run)> Jobs = new()
    {
        ["scheduling-no-show"] = ("*/5 * * * *", (sp, ct) => sp.GetRequiredService<NoShowJob>().RunAsync(ct)),
        ["notifications-send"] = ("* * * * *", (sp, ct) => sp.GetRequiredService<NotificationSendJob>().RunAsync(ct)),
        ["online-payments-expire"] = ("*/30 * * * *", (sp, ct) => sp.GetRequiredService<OnlinePaymentExpiryJob>().RunAsync(ct)),
        ["messaging-cleanup"] = ("0 3 * * *", (sp, ct) => sp.GetRequiredService<MessagingCleanupJob>().RunAsync(ct)),
    };

    public static IServiceCollection AddShifaJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<NoShowJob>();
        services.AddScoped<NotificationSendJob>();
        services.AddScoped<OnlinePaymentExpiryJob>();
        services.AddScoped<MessagingCleanupJob>();

        if (!configuration.GetValue("Jobs:Enabled", true))
            return services;

        services.AddHangfire((sp, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                sp.GetRequiredService<IConfiguration>().GetConnectionString(ModuleDbContextExtensions.ConnectionStringName),
                new SqlServerStorageOptions { SchemaName = "hangfire", PrepareSchemaIfNecessary = true, QueuePollInterval = TimeSpan.FromSeconds(15) }));
        services.AddHangfireServer(options => options.WorkerCount = Math.Max(2, Environment.ProcessorCount));
        return services;
    }

    public static WebApplication UseShifaJobs(this WebApplication app)
    {
        if (app.Configuration.GetValue("Jobs:Enabled", true))
        {
            var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
            recurring.AddOrUpdate<NoShowJob>("scheduling-no-show", j => j.RunAsync(CancellationToken.None), Jobs["scheduling-no-show"].Cron);
            recurring.AddOrUpdate<NotificationSendJob>("notifications-send", j => j.RunAsync(CancellationToken.None), Jobs["notifications-send"].Cron);
            recurring.AddOrUpdate<OnlinePaymentExpiryJob>("online-payments-expire", j => j.RunAsync(CancellationToken.None), Jobs["online-payments-expire"].Cron);
            recurring.AddOrUpdate<MessagingCleanupJob>("messaging-cleanup", j => j.RunAsync(CancellationToken.None), Jobs["messaging-cleanup"].Cron);

            if (app.Environment.IsDevelopment())
                app.UseHangfireDashboard("/hangfire");   // default authorization: local requests only
        }

        if (app.Environment.IsDevelopment())
        {
            // Development only: run a job now instead of waiting for its schedule (smoke tests).
            app.MapPost("/dev/jobs/{name}", async (string name, IServiceProvider sp, CancellationToken ct) =>
                Jobs.TryGetValue(name, out var job) ? Results.Ok(new { name, result = await job.Run(sp, ct) }) : Results.NotFound())
                .AllowAnonymous();
        }

        return app;
    }
}
