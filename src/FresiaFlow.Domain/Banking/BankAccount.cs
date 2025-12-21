namespace FresiaFlow.Domain.Banking;

/// <summary>
/// Representa una cuenta bancaria de la empresa.
/// </summary>
public class BankAccount
{
    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public string AccountType { get; private set; } = string.Empty;
    public string? ExternalAccountId { get; private set; } // ID del proveedor Open Banking
    public DateTime LastSyncAt { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<BankTransaction> _transactions = new();
    public IReadOnlyCollection<BankTransaction> Transactions => _transactions.AsReadOnly();

    private BankAccount() { } // EF Core

    public BankAccount(
        string accountNumber,
        string bankName,
        string accountType,
        string? externalAccountId = null)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        BankName = bankName;
        AccountType = accountType;
        ExternalAccountId = externalAccountId;
        IsActive = true;
        LastSyncAt = DateTime.UtcNow;
    }

    public void AddTransaction(BankTransaction transaction)
    {
        if (!_transactions.Any(t => t.ExternalTransactionId == transaction.ExternalTransactionId))
        {
            _transactions.Add(transaction);
        }
    }

    public void MarkSyncCompleted()
    {
        LastSyncAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

