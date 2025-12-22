namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para actualizar una factura recibida.
/// Todos los campos son opcionales, solo se actualizan los que se proporcionen.
/// </summary>
public class UpdateInvoiceDto
{
    /// <summary>
    /// Número de factura.
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Nombre del proveedor.
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// NIF/CIF del proveedor.
    /// </summary>
    public string? SupplierTaxId { get; set; }

    /// <summary>
    /// Fecha de emisión.
    /// </summary>
    public DateTime? IssueDate { get; set; }

    /// <summary>
    /// Fecha de vencimiento.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Importe total.
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Importe de impuestos (IVA).
    /// </summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// Base imponible (subtotal).
    /// </summary>
    public decimal? SubtotalAmount { get; set; }

    /// <summary>
    /// Moneda (EUR, USD, etc.).
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Notas adicionales.
    /// </summary>
    public string? Notes { get; set; }
}

