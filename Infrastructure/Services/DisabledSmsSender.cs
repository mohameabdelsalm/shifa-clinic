using Core.Interfaces.Common;

namespace Infrastructure.Services;

/// <summary>
/// Sms:Provider=Disabled (ADR-017): no SMS provider chosen yet. <see cref="IsEnabled"/> is false, so portal sign-in codes and
/// SMS notifications are skipped on purpose; calling <see cref="SendAsync"/> anyway is a bug.
/// </summary>
public sealed class DisabledSmsSender : ISmsSender
{
    public bool IsEnabled => false;

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("SMS is disabled (Sms:Provider=Disabled).");
}
