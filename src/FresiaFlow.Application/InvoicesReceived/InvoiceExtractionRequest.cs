namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Parámetros para la extracción estructurada. Permite cambiar de modelo sin duplicar código.
/// </summary>
public sealed class InvoiceExtractionRequest
{
    public InvoiceExtractionRequest(
        string ocrText,
        bool useHighPrecisionModel = false,
        string? cacheKey = null)
    {
        OcrText = ocrText;
        UseHighPrecisionModel = useHighPrecisionModel;
        CacheKey = cacheKey;
    }

    public string OcrText { get; }
    public bool UseHighPrecisionModel { get; }
    public string? CacheKey { get; }
}

