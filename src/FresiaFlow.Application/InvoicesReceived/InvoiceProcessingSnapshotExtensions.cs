using System.Text.Json;
using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Métodos auxiliares para reconstruir DTOs a partir del snapshot persistido.
/// </summary>
public static class InvoiceProcessingSnapshotExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static OcrResultDto? ToOcrResult(this InvoiceProcessingSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.OcrText))
        {
            return null;
        }

        var pages = string.IsNullOrWhiteSpace(snapshot.OcrLayoutJson)
            ? Array.Empty<OcrPageLayoutDto>()
            : JsonSerializer.Deserialize<IReadOnlyList<OcrPageLayoutDto>>(snapshot.OcrLayoutJson, SerializerOptions)
              ?? Array.Empty<OcrPageLayoutDto>();

        return new OcrResultDto(
            snapshot.OcrText,
            snapshot.OcrConfidence ?? 0m,
            pages);
    }

    public static DocumentClassificationResultDto? ToClassificationResult(this InvoiceProcessingSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.DocumentType))
        {
            return null;
        }

        return new DocumentClassificationResultDto
        {
            DocumentType = snapshot.DocumentType,
            Language = snapshot.DocumentLanguage ?? "es",
            SupplierGuess = snapshot.SupplierCandidate,
            Confidence = snapshot.ClassificationConfidence ?? 0m,
            RawJson = snapshot.ClassificationJson
        };
    }

    public static InvoiceExtractionResultDto? ToExtractionResult(this InvoiceProcessingSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.ExtractionJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<InvoiceExtractionResultDto>(
                snapshot.ExtractionJson,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

