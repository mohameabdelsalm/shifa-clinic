using System.Collections.Concurrent;
using Shifa.Core.Interfaces.Common;

namespace Shifa.Infrastructure.Services;

public sealed record SentSms(DateTimeOffset At, string PhoneNumber, string Message);

/// <summary>Keeps the last messages in memory so tests and developers can read them. Nothing is sent or logged.</summary>
public sealed class InMemorySmsOutbox
{
    private const int MaxMessages = 200;
    private readonly ConcurrentQueue<SentSms> _messages = new();

    public void Add(SentSms sms)
    {
        _messages.Enqueue(sms);
        while (_messages.Count > MaxMessages && _messages.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<SentSms> Messages => _messages.ToArray();
    public SentSms? LastTo(string phoneNumber) => _messages.Reverse().FirstOrDefault(m => m.PhoneNumber == phoneNumber);
}

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
