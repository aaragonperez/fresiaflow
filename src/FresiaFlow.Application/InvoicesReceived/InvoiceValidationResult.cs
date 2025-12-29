using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Resultado de las validaciones deterministas posteriores a la extracción.
/// </summary>
public sealed class InvoiceValidationResult
{
    public InvoiceValidationResult(
        InvoiceValidationStatus status,
        IReadOnlyCollection<string> errors)
    {
        Status = status;
        Errors = errors;
    }

    public InvoiceValidationStatus Status { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public bool IsOk => Status == InvoiceValidationStatus.Ok;
}

