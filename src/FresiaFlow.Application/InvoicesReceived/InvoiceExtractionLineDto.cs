using System.Text.Json.Serialization;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Línea de detalle extraída de una factura.
/// Mapea la estructura JSON devuelta por OpenAI según CompleteExtractionTemplate.
/// </summary>
public class InvoiceExtractionLineDto
{
    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("taxRate")]
    public decimal? TaxRate { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal LineTotal { get; set; }
}

