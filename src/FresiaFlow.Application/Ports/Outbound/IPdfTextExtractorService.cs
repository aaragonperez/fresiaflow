namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para extracción de texto de PDFs.
/// </summary>
public interface IPdfTextExtractorService
{
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
}

