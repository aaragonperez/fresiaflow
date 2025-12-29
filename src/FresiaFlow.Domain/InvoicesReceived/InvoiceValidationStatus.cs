namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Estado determinista del documento tras la validación sin IA.
/// </summary>
public enum InvoiceValidationStatus
{
    Pending = 0,
    Ok = 1,
    Doubtful = 2
}

