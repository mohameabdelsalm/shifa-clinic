namespace Infrastructure.Services;

public sealed record SentSms(DateTimeOffset At, string PhoneNumber, string Message);
