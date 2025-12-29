using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para generar asientos contables automáticamente desde facturas.
/// </summary>
public interface IGenerateAccountingEntriesUseCase
{
    /// <summary>
    /// Genera asientos contables para todas las facturas que aún no tienen asiento.
    /// </summary>
    Task<GenerateAccountingEntriesResult> ExecuteAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Genera asientos contables para una factura específica.
    /// </summary>
    Task<AccountingEntry?> GenerateForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Regenera un asiento eliminando el existente y creando uno nuevo.
    /// </summary>
    Task<AccountingEntry?> RegenerateForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Regenera asientos para múltiples facturas.
    /// </summary>
    Task<GenerateAccountingEntriesResult> RegenerateForInvoicesAsync(IEnumerable<Guid> invoiceIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Información sobre una factura que no pudo generar asiento.
/// </summary>
public record FailedInvoiceInfo(
    Guid InvoiceId,
    string InvoiceNumber,
    string SupplierName,
    string Reason);

/// <summary>
/// Resultado de la generación de asientos contables.
/// </summary>
public record GenerateAccountingEntriesResult(
    int TotalProcessed,
    int SuccessCount,
    int ErrorCount,
    List<string> Errors,
    List<FailedInvoiceInfo> FailedInvoices);

