using System.Text.Json.Serialization;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Resultado de la extracción semántica de una factura.
/// Mapea la estructura JSON devuelta por OpenAI según CompleteExtractionTemplate.
/// </summary>
public class InvoiceExtractionResultDto
{
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("supplierName")]
    public string SupplierName { get; set; } = string.Empty;

    [JsonPropertyName("supplierTaxId")]
    public string? SupplierTaxId { get; set; }

    [JsonPropertyName("issueDate")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public string? DueDate { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal? TaxAmount { get; set; }

    [JsonPropertyName("subtotalAmount")]
    public decimal? SubtotalAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EUR";

    [JsonPropertyName("lines")]
    public List<InvoiceExtractionLineDto> Lines { get; set; } = new();

    /// <summary>
    /// Convierte la fecha string a DateTime.
    /// </summary>
    public DateTime GetIssueDate()
    {
        return ParseDate(IssueDate);
    }

    /// <summary>
    /// Convierte la fecha string a DateTime?.
    /// </summary>
    public DateTime? GetDueDate()
    {
        if (string.IsNullOrWhiteSpace(DueDate)) return null;
        return ParseDate(DueDate);
    }

    private static DateTime ParseDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return DateTime.UtcNow;

        // Intentar formato ISO (YYYY-MM-DD) - asumir UTC
        if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, 
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, 
            out var isoDate))
            return isoDate;

        // Intentar formato español (DD/MM/YYYY) - asumir UTC
        if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, 
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, 
            out var esDate))
            return esDate;

        // Intentar parseo genérico - asumir UTC
        if (DateTime.TryParse(dateStr, null, 
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, 
            out var parsedDate))
            return parsedDate;

        return DateTime.UtcNow;
    }
}

