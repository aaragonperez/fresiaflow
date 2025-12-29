using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.Accounting;

/// <summary>
/// Línea de un asiento contable (debe o haber).
/// </summary>
public class AccountingEntryLine
{
    public Guid Id { get; private set; }
    public Guid AccountingEntryId { get; private set; } // FK al asiento
    public Guid AccountingAccountId { get; private set; } // FK a la cuenta contable
    public EntrySide Side { get; private set; } // Debe o Haber
    public Money Amount { get; private set; } = null!; // Importe
    public string? Description { get; private set; } // Descripción de la línea

    private AccountingEntryLine() { }

    public AccountingEntryLine(
        Guid accountingEntryId,
        Guid accountingAccountId,
        EntrySide side,
        Money amount,
        string? description = null)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (amount.Value <= 0)
            throw new ArgumentException("El importe debe ser mayor que cero.", nameof(amount));

        Id = Guid.NewGuid();
        AccountingEntryId = accountingEntryId;
        AccountingAccountId = accountingAccountId;
        Side = side;
        Amount = amount;
        Description = description;
    }

    /// <summary>
    /// Actualiza el importe de la línea.
    /// </summary>
    public void UpdateAmount(Money amount)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (amount.Value <= 0)
            throw new ArgumentException("El importe debe ser mayor que cero.", nameof(amount));

        Amount = amount;
    }

    /// <summary>
    /// Actualiza la descripción de la línea.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}

/// <summary>
/// Lado del asiento (debe o haber).
/// </summary>
public enum EntrySide
{
    Debit = 1,  // Debe
    Credit = 2  // Haber
}

