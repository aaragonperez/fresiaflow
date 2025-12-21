using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.Invoices;

/// <summary>
/// Entidad raíz del agregado de facturas.
/// Representa una factura recibida o emitida por la empresa.
/// </summary>
public class Invoice
{
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Money Amount { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public string SupplierName { get; private set; } = string.Empty;
    public string? FilePath { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReconciledAt { get; private set; }
    public Guid? ReconciledWithTransactionId { get; private set; }

    private Invoice() { } // EF Core

    public Invoice(
        string invoiceNumber,
        DateTime issueDate,
        DateTime? dueDate,
        Money amount,
        string supplierName,
        string? filePath = null)
    {
        Id = Guid.NewGuid();
        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        DueDate = dueDate;
        Amount = amount;
        SupplierName = supplierName;
        FilePath = filePath;
        Status = InvoiceStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid(Guid bankTransactionId)
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("La factura ya está marcada como pagada.");

        Status = InvoiceStatus.Paid;
        ReconciledAt = DateTime.UtcNow;
        ReconciledWithTransactionId = bankTransactionId;
    }

    public void MarkAsOverdue()
    {
        if (Status == InvoiceStatus.Paid)
            return;

        if (DueDate.HasValue && DueDate.Value < DateTime.UtcNow)
        {
            Status = InvoiceStatus.Overdue;
        }
    }

    public bool CanBeReconciledWith(decimal transactionAmount, DateTime transactionDate)
    {
        return InvoiceRules.CanReconcile(this, transactionAmount, transactionDate);
    }
}

