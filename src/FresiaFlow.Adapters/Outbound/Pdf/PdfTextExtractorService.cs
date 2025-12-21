using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace FresiaFlow.Adapters.Outbound.Pdf;

/// <summary>
/// Servicio de extracción de texto de PDFs usando PdfPig.
/// </summary>
public class PdfTextExtractorService : IPdfTextExtractorService
{
    private readonly ILogger<PdfTextExtractorService> _logger;

    public PdfTextExtractorService(ILogger<PdfTextExtractorService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"El archivo PDF no existe: {filePath}");
        }

        _logger.LogDebug("Extrayendo texto del PDF: {FilePath}", filePath);

        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfDocument.Open(filePath);
                var textBuilder = new System.Text.StringBuilder();

                foreach (var page in document.GetPages())
                {
                    var text = page.Text;
                    textBuilder.AppendLine(text);
                    textBuilder.AppendLine(); // Separador entre páginas
                }

                var result = textBuilder.ToString();
                _logger.LogDebug("Texto extraído: {Length} caracteres", result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto del PDF: {FilePath}", filePath);
                throw new InvalidOperationException($"Error al procesar el PDF: {ex.Message}", ex);
            }
        }, cancellationToken);
    }
}

