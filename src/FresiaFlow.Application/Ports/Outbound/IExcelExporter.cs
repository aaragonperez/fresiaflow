using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto para exportar facturas emitidas a Excel.
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// Exporta facturas emitidas a un archivo Excel.
    /// </summary>
    Task<byte[]> ExportToExcelAsync(
        List<IssuedInvoice> invoices,
        CancellationToken cancellationToken = default);
}

