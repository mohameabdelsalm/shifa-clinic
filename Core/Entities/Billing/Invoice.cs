using Core.Entities.Common;
using Core.Enums.Billing;

namespace Core.Entities.Billing;

/// <summary>
/// An invoice (ADR-011). Never edited after creation: money changes only through the methods below, which keep the
/// invariant <c>0 ≤ PaidAmount</c> and <c>PaidAmount + CreditedAmount ≤ Total</c>. Every method also updates this row,
/// so its rowversion makes two money operations on the same invoice serialize (the second gets 409 and retries).
/// </summary>
public class Invoice : TenantOwnedEntity
{
    public long BranchId { get; set; }
    public long PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public InvoiceKindEnum Kind { get; set; }

    /// <summary>INV-yyyy-000001 (unique per tenant); <see cref="Year"/> and <see cref="Sequence"/> build it.</summary>
    public string Number { get; set; } = "";
    public int Year { get; set; }
    public int Sequence { get; set; }

    public InvoiceStatusEnum Status { get; private set; } = InvoiceStatusEnum.Open;
    public string CurrencyCode { get; set; } = "EGP";
    public DateTimeOffset IssuedAt { get; set; }

    public decimal Subtotal { get; private set; }

    /// <summary>Always 0 for now (no discounts, ADR-011).</summary>
    public decimal Discount { get; private set; }

    /// <summary>0 until the Egyptian tax rules are confirmed (assumption L6).</summary>
    public decimal Tax { get; private set; }

    public decimal Total { get; private set; }

    /// <summary>Payments minus refunds.</summary>
    public decimal PaidAmount { get; private set; }
    public decimal CreditedAmount { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }
    public string? VoidReason { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    public decimal Balance => Total - CreditedAmount - PaidAmount;

    /// <summary>Sets the totals once, from the item line totals (before the first save).</summary>
    public void SetTotals(decimal subtotal)
    {
        if (Total != 0 || PaidAmount != 0 || CreditedAmount != 0)
            throw new InvalidOperationException("Invoice totals are set once, before any money is recorded.");

        Subtotal = subtotal;
        Total = Subtotal - Discount + Tax;
        UpdateStatus();
    }

    /// <summary>False when the invoice is void or the amount is not in (0, Balance].</summary>
    public bool TryApplyPayment(decimal amount)
    {
        if (Status == InvoiceStatusEnum.Void || amount <= 0 || amount > Balance)
            return false;

        PaidAmount += amount;
        UpdateStatus();
        return true;
    }

    /// <summary>False when the amount is not in (0, PaidAmount].</summary>
    public bool TryApplyRefund(decimal amount)
    {
        if (amount <= 0 || amount > PaidAmount)
            return false;

        PaidAmount -= amount;
        UpdateStatus();
        return true;
    }

    /// <summary>False when the invoice is void or the amount is not in (0, Balance] (refund paid money first).</summary>
    public bool TryApplyCredit(decimal amount)
    {
        if (Status == InvoiceStatusEnum.Void || amount <= 0 || amount > Balance)
            return false;

        CreditedAmount += amount;
        UpdateStatus();
        return true;
    }

    /// <summary>Only when nothing was paid or credited.</summary>
    public bool TryVoid(string reason, DateTimeOffset now)
    {
        if (Status == InvoiceStatusEnum.Void || PaidAmount != 0 || CreditedAmount != 0)
            return false;

        Status = InvoiceStatusEnum.Void;
        VoidedAt = now;
        VoidReason = reason;
        return true;
    }

    private void UpdateStatus()
    {
        if (Status == InvoiceStatusEnum.Void)
            return;

        Status = Balance == 0 ? InvoiceStatusEnum.Paid
            : PaidAmount > 0 ? InvoiceStatusEnum.PartiallyPaid
            : InvoiceStatusEnum.Open;
    }
}
