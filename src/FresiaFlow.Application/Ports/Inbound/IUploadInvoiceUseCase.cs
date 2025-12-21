using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada (Inbound Port) para subir y procesar facturas.
/// Define el contrato que la UI/API debe cumplir.
/// </summary>
public interface IUploadInvoiceUseCase
{
    /// <summary>
    /// Sube una factura desde un archivo PDF y la procesa.
    /// </summary>
    Task<InvoiceResult> ExecuteAsync(UploadInvoiceCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Comando para subir una factura.
/// </summary>
public record UploadInvoiceCommand(
    Stream FileStream,
    string FileName,
    string ContentType
);

/// <summary>
/// Resultado de la operación de subida de factura.
/// </summary>
public record InvoiceResult(
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string SupplierName,
    bool RequiresReview
);

