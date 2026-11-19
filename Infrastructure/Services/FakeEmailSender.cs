using Core.Interfaces.Common;

namespace Infrastructure.Services;

/// <summary>The Development <see cref="IEmailSender"/>: keeps messages in <see cref="InMemoryEmailOutbox"/>.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly InMemoryEmailOutbox _outbox;
    private readonly TimeProvider _time;

    public FakeEmailSender(InMemoryEmailOutbox outbox, TimeProvider time)
    {
        _outbox = outbox;
        _time = time;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _outbox.Add(new SentEmail(_time.GetUtcNow(), to, subject, body));
        return Task.CompletedTask;
    }
}
