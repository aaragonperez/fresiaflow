using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.Banking;

/// <summary>
/// Representa una transacción bancaria.
/// </summary>
public class BankTransaction
{
    public Guid Id { get; private set; }
    public Guid BankAccountId { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public string? ExternalTransactionId { get; private set; } // ID del proveedor Open Banking
    public bool IsReconciled { get; private set; }
    public Guid? ReconciledWithInvoiceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BankTransaction() { } // EF Core

    public BankTransaction(
        Guid bankAccountId,
        DateTime transactionDate,
        Money amount,
        string description,
        string? reference = null,
        string? externalTransactionId = null)
    {
        Id = Guid.NewGuid();
        BankAccountId = bankAccountId;
        TransactionDate = transactionDate;
        Amount = amount;
        Description = description;
        Reference = reference;
        ExternalTransactionId = externalTransactionId;
        IsReconciled = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void ReconcileWithInvoice(Guid invoiceId)
    {
        if (IsReconciled)
            throw new InvalidOperationException("La transacción ya está conciliada.");

        IsReconciled = true;
        ReconciledWithInvoiceId = invoiceId;
    }

    public void Unreconcile()
    {
        IsReconciled = false;
        ReconciledWithInvoiceId = null;
    }
}

