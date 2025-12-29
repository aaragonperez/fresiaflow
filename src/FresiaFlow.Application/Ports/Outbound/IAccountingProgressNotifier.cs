namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto para notificar progreso de generación de asientos contables.
/// </summary>
public interface IAccountingProgressNotifier
{
    Task NotifyAsync(AccountingProgressUpdate update, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO de progreso para generación de asientos contables.
/// </summary>
public class AccountingProgressUpdate
{
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int Percentage { get; set; }
    public string Status { get; set; } = string.Empty; // "generating", "completed", "error"
    public string? Message { get; set; }
    public string? CurrentInvoiceNumber { get; set; }
    public string? CurrentInvoiceSupplier { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string? CurrentError { get; set; }
}

