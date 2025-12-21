namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Resultado de la extracción semántica de una factura.
/// </summary>
public class InvoiceExtractionResultDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierTaxId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? SubtotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public List<InvoiceExtractionLineDto> Lines { get; set; } = new();
}

