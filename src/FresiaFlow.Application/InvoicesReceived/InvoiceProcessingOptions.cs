namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Parámetros operativos para el pipeline OCR + IA híbrido.
/// </summary>
public class InvoiceProcessingOptions
{
    public const string SectionName = "InvoiceProcessing";

    /// <summary>
    /// Confianza mínima del OCR antes de disparar el fallback caro.
    /// </summary>
    public decimal OcrConfidenceThreshold { get; set; } = 0.85m;

    /// <summary>
    /// Tolerancia (en moneda) para validar que subtotal + IVA - IRPF = total.
    /// </summary>
    public decimal TotalTolerance { get; set; } = 0.5m;

    /// <summary>
    /// Versión del esquema de extracción para poder invalidar caché de forma segura.
    /// </summary>
    public string ExtractionVersion { get; set; } = "1.0";

    /// <summary>
    /// Permite habilitar/deshabilitar el fallback sin re desplegar código.
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Cota blanda para auditar que el fallback no supere el 15% de los documentos.
    /// </summary>
    public decimal FallbackMaxShare { get; set; } = 0.15m;
}

