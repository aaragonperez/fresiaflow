namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Cache persistente del pipeline OCR + IA para evitar reprocesar documentos.
/// </summary>
public class InvoiceProcessingSnapshot
{
    public Guid Id { get; private set; }
    public string SourceFilePath { get; private set; } = string.Empty;
    public string SourceFileHash { get; private set; } = string.Empty;

    // OCR
    public string? OcrText { get; private set; }
    public string? OcrLayoutJson { get; private set; }
    public decimal? OcrConfidence { get; private set; }
    public DateTime? OcrCompletedAt { get; private set; }

    // Clasificación
    public string? DocumentType { get; private set; }
    public string? DocumentLanguage { get; private set; }
    public string? SupplierCandidate { get; private set; }
    public decimal? ClassificationConfidence { get; private set; }
    public string? ClassificationJson { get; private set; }
    public DateTime? ClassificationCompletedAt { get; private set; }

    // Extracción estructurada
    public string? ExtractionJson { get; private set; }
    public string? ExtractionVersion { get; private set; }
    public string? ExtractionHash { get; private set; }
    public decimal? ExtractionConfidence { get; private set; }
    public DateTime? ExtractionCompletedAt { get; private set; }

    // Validación determinista
    public InvoiceValidationStatus ValidationStatus { get; private set; } = InvoiceValidationStatus.Pending;
    public string? ValidationErrors { get; private set; }
    public DateTime? ValidationCompletedAt { get; private set; }

    // Fallback
    public bool FallbackTriggered { get; private set; }
    public string? FallbackReason { get; private set; }
    public DateTime? FallbackCompletedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private InvoiceProcessingSnapshot()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public InvoiceProcessingSnapshot(string sourceFilePath, string sourceFileHash)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("La ruta del archivo origen es obligatoria.", nameof(sourceFilePath));

        if (string.IsNullOrWhiteSpace(sourceFileHash))
            throw new ArgumentException("El hash del archivo es obligatorio.", nameof(sourceFileHash));

        Id = Guid.NewGuid();
        SourceFilePath = sourceFilePath;
        SourceFileHash = sourceFileHash;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SaveOcrPayload(string text, decimal confidence, string layoutJson)
    {
        if (!string.IsNullOrWhiteSpace(OcrText))
        {
            return; // Idempotente: no volver a ejecutar OCR caro si ya existe.
        }

        OcrText = text;
        OcrConfidence = confidence;
        OcrLayoutJson = layoutJson;
        OcrCompletedAt = DateTime.UtcNow;
        Touch();
    }

    public void SaveClassificationPayload(
        string documentType,
        string language,
        string? supplierCandidate,
        decimal confidence,
        string rawJson)
    {
        if (!string.IsNullOrWhiteSpace(DocumentType))
        {
            return;
        }

        DocumentType = documentType;
        DocumentLanguage = language;
        SupplierCandidate = supplierCandidate;
        ClassificationConfidence = confidence;
        ClassificationJson = rawJson;
        ClassificationCompletedAt = DateTime.UtcNow;
        Touch();
    }

    public void SaveExtractionPayload(
        string extractionJson,
        string extractionVersion,
        string extractionHash,
        decimal? extractionConfidence = null)
    {
        ExtractionJson = extractionJson;
        ExtractionVersion = extractionVersion;
        ExtractionHash = extractionHash;
        ExtractionConfidence = extractionConfidence;
        ExtractionCompletedAt = DateTime.UtcNow;
        Touch();
    }

    public void SaveValidationResult(InvoiceValidationStatus status, string? errors)
    {
        ValidationStatus = status;
        ValidationErrors = errors;
        ValidationCompletedAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkFallback(string reason)
    {
        FallbackTriggered = true;
        FallbackReason = reason;
        FallbackCompletedAt = DateTime.UtcNow;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}

