using Core.Interfaces.Common;

namespace Infrastructure.Services;

/// <summary>The local <see cref="ISmsSender"/> (ADR-006). Replace it with a provider implementation in production.</summary>
public sealed class FakeSmsSender : ISmsSender
{
    private readonly InMemorySmsOutbox _outbox;
    private readonly TimeProvider _time;

    public FakeSmsSender(InMemorySmsOutbox outbox, TimeProvider time)
    {
        _outbox = outbox;
        _time = time;
    }

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _outbox.Add(new SentSms(_time.GetUtcNow(), phoneNumber, message));
        return Task.CompletedTask;
    }
}
