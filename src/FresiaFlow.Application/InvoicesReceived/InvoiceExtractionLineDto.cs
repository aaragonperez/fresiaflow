namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Línea de detalle extraída de una factura.
/// </summary>
public class InvoiceExtractionLineDto
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}

