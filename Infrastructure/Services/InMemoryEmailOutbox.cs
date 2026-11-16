using System.Collections.Concurrent;

namespace Infrastructure.Services;

/// <summary>Keeps the last e-mails in memory (Development only, GET /dev/email). Nothing is sent or logged.</summary>
public sealed class InMemoryEmailOutbox
{
    private const int MaxMessages = 200;
    private readonly ConcurrentQueue<SentEmail> _messages = new();

    public void Add(SentEmail email)
    {
        _messages.Enqueue(email);
        while (_messages.Count > MaxMessages && _messages.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<SentEmail> Messages => _messages.ToArray();
}
