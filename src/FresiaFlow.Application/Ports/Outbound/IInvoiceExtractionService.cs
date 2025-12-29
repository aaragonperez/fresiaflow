using FresiaFlow.Application.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para extracción semántica de facturas usando IA.
/// </summary>
public interface IInvoiceExtractionService
{
    Task<InvoiceExtractionResultDto> ExtractInvoiceDataAsync(
        InvoiceExtractionRequest request,
        CancellationToken cancellationToken = default);
}

