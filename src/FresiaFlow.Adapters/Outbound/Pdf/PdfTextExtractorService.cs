using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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

    public async Task<OcrResultDto> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"El archivo PDF no existe: {filePath}");
        }

        _logger.LogDebug("Extrayendo texto + layout ligero del PDF: {FilePath}", filePath);

        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfDocument.Open(filePath);
                var textBuilder = new System.Text.StringBuilder();
                var pages = new List<OcrPageLayoutDto>();

                foreach (var page in document.GetPages())
                {
                    var text = page.Text;
                    textBuilder.AppendLine(text);
                    textBuilder.AppendLine(); // Separador entre páginas

                    var blocks = page.Letters
                        .Select(letter => new OcrTextBlockDto(
                            letter.Value.ToString(),
                            (decimal)letter.GlyphRectangle.Left,
                            (decimal)letter.GlyphRectangle.Bottom,
                            (decimal)letter.GlyphRectangle.Width,
                            (decimal)letter.GlyphRectangle.Height,
                            0.95m))
                        .Take(1500) // mantener layout ligero para no inflar base de datos/coste
                        .ToList();

                    pages.Add(new OcrPageLayoutDto(
                        page.Number,
                        (decimal)page.Width,
                        (decimal)page.Height,
                        blocks));
                }

                var result = textBuilder.ToString();
                _logger.LogDebug("OCR extraído: {Length} caracteres", result.Length);

                var confidence = CalculateConfidenceScore(result.Length, pages.Sum(p => p.Blocks.Count));
                return new OcrResultDto(result, confidence, pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto del PDF: {FilePath}", filePath);
                throw new InvalidOperationException($"Error al procesar el PDF: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    private static decimal CalculateConfidenceScore(int textLength, int blocks)
    {
        if (textLength <= 0)
        {
            return 0.3m;
        }

        var normalized = Math.Clamp(textLength / 600m, 0.5m, 1.2m);
        var densityBoost = Math.Clamp(blocks / 1500m, 0.3m, 0.5m);
        return Math.Clamp((decimal)normalized * 0.6m + (decimal)densityBoost, 0.55m, 0.98m);
    }
}

