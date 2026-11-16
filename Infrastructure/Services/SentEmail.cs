namespace Infrastructure.Services;

public sealed record SentEmail(DateTimeOffset At, string To, string Subject, string Body);
