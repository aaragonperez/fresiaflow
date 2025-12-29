using FresiaFlow.Application.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para extracción OCR de PDFs (texto + layout ligero).
/// </summary>
public interface IPdfTextExtractorService
{
    Task<OcrResultDto> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}

