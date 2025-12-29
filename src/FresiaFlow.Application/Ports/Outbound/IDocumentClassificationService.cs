using FresiaFlow.Application.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto para clasificar documentos con un modelo ligero.
/// </summary>
public interface IDocumentClassificationService
{
    Task<DocumentClassificationResultDto> ClassifyAsync(
        OcrResultDto ocr,
        CancellationToken cancellationToken = default);
}

