using System.Collections.Concurrent;

namespace Infrastructure.Services;

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
