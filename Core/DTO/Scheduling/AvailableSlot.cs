namespace Core.DTO.Scheduling;

/// <summary>A bookable start: occupies [StartUtc, EndUtc). LocalStart is the wall-clock time at the branch.</summary>
public sealed record AvailableSlot(DateTimeOffset StartUtc, DateTimeOffset EndUtc, DateTime LocalStart);
