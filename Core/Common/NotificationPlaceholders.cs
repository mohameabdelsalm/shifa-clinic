using Core.Enums.Notifications;

namespace Core.Common;

/// <summary>The values each message kind can show (ADR-015). Nothing medical is ever offered as a placeholder.</summary>
public static class NotificationPlaceholders
{
    public static readonly IReadOnlyList<string> Languages = ["ar", "en"];

    private static readonly string[] Appointment = ["patient", "clinic", "doctor", "branch", "date", "time"];
    private static readonly string[] Receipt = ["patient", "clinic", "amount", "currency", "invoice", "receipt"];

    public static IReadOnlyList<string> For(NotificationKindEnum kind) => kind == NotificationKindEnum.PaymentReceipt ? Receipt : Appointment;
}
