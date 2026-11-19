namespace Infrastructure.Configurations.Billing;

/// <summary>Index names the repos look for in unique-violation errors.</summary>
public static class BillingIndexes
{
    public const string VisitInvoice = "IX_Invoices_VisitAppointment";
    public const string OpenShift = "IX_CashierShifts_Open";
    public const string OnlineTransaction = "IX_Payments_OnlineTransaction";
    public const string Idempotency = "IX_BillingIdempotency_Key";
}
