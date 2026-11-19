using System.Net;
using Infrastructure.Data;
using Infrastructure.Services.Jobs;
using Microsoft.EntityFrameworkCore;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>
/// Outbox → handler → notifications → sending (ADR-005, ADR-015). The dispatcher and the sending job are run directly,
/// so the test controls when background work happens.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class NotificationFlowTests
{
    private readonly SqlServerFixture _db;

    public NotificationFlowTests(SqlServerFixture db)
    {
        _db = db;
    }

    private static async Task<List<(string Kind, string Status)>> NotificationsAsync(TestClinic c, long appointmentId) =>
        TestClinic.Ok(await c.Api.PostAsync("/api/notifications/search", c.Admin, new { appointmentId, pageNumber = 1, pageSize = 20 }))
            .GetProperty("items").EnumerateArray()
            .Select(n => (n.GetProperty("kind").GetString()!, n.GetProperty("status").GetString()!))
            .ToList();

    [Fact]
    public async Task A_booking_plans_confirmation_and_reminders_and_a_cancellation_replaces_them()
    {
        await using var c = await TestClinic.CreateAsync(_db);
        var appointmentId = await c.BookAsync("09:00");              // Monday 09:00, 25 hours ahead

        await c.DispatchOutboxAsync<SchedulingDbContext>();
        var planned = await NotificationsAsync(c, appointmentId);
        Assert.Equal(["AppointmentConfirmed", "AppointmentReminder24h", "AppointmentReminder2h"], planned.Select(n => n.Kind).Order());
        Assert.All(planned, n => Assert.Equal("Scheduled", n.Status));

        await c.JobAsync<NotificationSendJob>(job => job.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, c.Api.SmsCount(TestClinic.PatientPhone));                    // only the confirmation is due now
        Assert.Contains(await NotificationsAsync(c, appointmentId), n => n is ("AppointmentConfirmed", "Sent"));

        // Redelivering the same events changes nothing: each handler records the message in its inbox.
        await using (var scheduling = _db.CreateSchedulingContext(tenantId: null))
            await scheduling.OutboxMessages.ExecuteUpdateAsync(s => s.SetProperty(m => m.ProcessedAt, (DateTimeOffset?)null), TestContext.Current.CancellationToken);
        await c.DispatchOutboxAsync<SchedulingDbContext>();
        Assert.Equal(3, (await NotificationsAsync(c, appointmentId)).Count);

        TestClinic.Ok(await c.Api.PutAsync($"/api/appointments/{appointmentId}/cancel", c.Admin, new { reason = "Patient asked" }));
        await c.DispatchOutboxAsync<SchedulingDbContext>();
        var afterCancel = await NotificationsAsync(c, appointmentId);
        Assert.Equal(2, afterCancel.Count(n => n.Kind.Contains("Reminder") && n.Status == "Cancelled"));
        Assert.Contains(afterCancel, n => n is ("AppointmentCancelled", "Scheduled"));

        await c.JobAsync<NotificationSendJob>(job => job.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, c.Api.SmsCount(TestClinic.PatientPhone));
    }

    [Fact]
    public async Task With_the_feature_off_nothing_is_planned()
    {
        await using var c = await TestClinic.CreateAsync(_db);
        TestClinic.Ok(await c.Api.PutAsync("/api/platform/tenants/features", c.Platform,
            new { tenantId = c.TenantId, code = "Feature.Notifications", enabled = false }));

        var appointmentId = await c.BookAsync("09:00");
        await c.DispatchOutboxAsync<SchedulingDbContext>();

        Assert.Equal(0, await _db.CountAsync($"SELECT COUNT(*) FROM notifications.Notifications WHERE AppointmentId = {appointmentId}"));
        Assert.Equal(0, await _db.CountAsync("SELECT COUNT(*) FROM scheduling.OutboxMessages WHERE ProcessedAt IS NULL"));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.Api.PostAsync("/api/notifications/search", c.Admin, new { pageNumber = 1, pageSize = 5 })).Status);
    }
}
