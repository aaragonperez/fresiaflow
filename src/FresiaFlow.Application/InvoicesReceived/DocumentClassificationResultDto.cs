using System.Text.Json.Serialization;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Resultado del paso de clasificación ligera (LLM barato).
/// </summary>
public sealed class DocumentClassificationResultDto
{
    public string DocumentType { get; init; } = "unknown";
    public string Language { get; init; } = "es";
    public string? SupplierGuess { get; init; }
    public string? ProviderId { get; init; }
    public decimal Confidence { get; init; }
    public string? RawJson { get; init; }

    [JsonIgnore]
    public bool IsInvoice =>
        DocumentType.Equals("invoice", StringComparison.OrdinalIgnoreCase) ||
        DocumentType.Equals("factura", StringComparison.OrdinalIgnoreCase);
}

