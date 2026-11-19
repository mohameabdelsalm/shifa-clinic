namespace Infrastructure.Services.Messaging;

/// <summary>The "Outbox" configuration section.</summary>
public sealed class OutboxOptions
{
    public const string Section = "Outbox";

    /// <summary>Turn the dispatcher off on an instance (e.g. a maintenance run). Messages wait in the outbox meanwhile.</summary>
    public bool Enabled { get; set; } = true;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);
    public int BatchSize { get; set; } = 20;
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>After this many failed attempts a message is poisoned (kept, logged, not retried).</summary>
    public int MaxAttempts { get; set; } = 10;
}
