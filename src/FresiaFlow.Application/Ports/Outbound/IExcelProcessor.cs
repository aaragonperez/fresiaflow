using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto para procesar archivos Excel con facturas emitidas.
/// </summary>
public interface IExcelProcessor
{
    /// <summary>
    /// Procesa un archivo Excel y extrae las facturas emitidas.
    /// </summary>
    Task<List<IssuedInvoiceData>> ProcessExcelAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO con los datos de una factura emitida extraída de Excel.
/// </summary>
public record IssuedInvoiceData(
    string Series,
    DateTime IssueDate,
    string InvoiceNumber,
    decimal TaxableBase,
    decimal TaxAmount,
    decimal TotalAmount,
    string ClientId,
    string ClientTaxId,
    string ClientName,
    string Address,
    string City,
    string PostalCode,
    string Province,
    string Country
);

