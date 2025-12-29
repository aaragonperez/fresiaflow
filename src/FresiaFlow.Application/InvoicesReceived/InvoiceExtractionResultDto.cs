using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    [JsonPropertyName("taxRate")]
    public decimal? TaxRate { get; set; }

    [JsonPropertyName("irpfAmount")]
    public decimal? IrpfAmount { get; set; }

    [JsonPropertyName("irpfRate")]
    public decimal? IrpfRate { get; set; }

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

        // Intentar formato español completo: "1 DE SEPTIEMBRE DE 2022", "15 DE ENERO DE 2024", etc.
        var spanishMonths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "ENERO", 1 }, { "FEBRERO", 2 }, { "MARZO", 3 }, { "ABRIL", 4 },
            { "MAYO", 5 }, { "JUNIO", 6 }, { "JULIO", 7 }, { "AGOSTO", 8 },
            { "SEPTIEMBRE", 9 }, { "OCTUBRE", 10 }, { "NOVIEMBRE", 11 }, { "DICIEMBRE", 12 }
        };

        // Patrón: "1 DE SEPTIEMBRE DE 2022" o "15 DE ENERO DE 2024"
        var pattern = @"(\d{1,2})\s+DE\s+(\w+)\s+DE\s+(\d{4})";
        var match = Regex.Match(dateStr, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var day = int.Parse(match.Groups[1].Value);
            var monthName = match.Groups[2].Value.ToUpperInvariant();
            var year = int.Parse(match.Groups[3].Value);

            if (spanishMonths.TryGetValue(monthName, out var month))
            {
                try
                {
                    var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                    return date;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Fecha inválida, continuar con otros métodos
                }
            }
        }

        // Intentar parseo genérico - asumir UTC
        if (DateTime.TryParse(dateStr, null, 
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, 
            out var parsedDate))
            return parsedDate;

        return DateTime.UtcNow;
    }
}

