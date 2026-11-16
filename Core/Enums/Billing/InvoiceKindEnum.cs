namespace Core.Enums.Billing;

public enum InvoiceKindEnum
{
    /// <summary>Created automatically at check-in; at most one per appointment.</summary>
    Visit = 1,

    /// <summary>Created by reception for additional services.</summary>
    Extra,
}
