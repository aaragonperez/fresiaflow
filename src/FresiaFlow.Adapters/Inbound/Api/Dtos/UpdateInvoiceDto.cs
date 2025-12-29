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

    /// <summary>
    /// Fecha de recepción contable.
    /// </summary>
    public DateTime? ReceivedDate { get; set; }

    /// <summary>
    /// Dirección fiscal del proveedor.
    /// </summary>
    public string? SupplierAddress { get; set; }

    /// <summary>
    /// Tipo impositivo de IVA aplicado a la factura (ej: 21, 10, 4).
    /// </summary>
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// Importe de retención IRPF (se resta del total).
    /// </summary>
    public decimal? IrpfAmount { get; set; }

    /// <summary>
    /// Tipo de retención IRPF aplicado (ej: 15, 7).
    /// </summary>
    public decimal? IrpfRate { get; set; }

    /// <summary>
    /// Líneas de detalle a reemplazar en la factura.
    /// </summary>
    public List<UpdateInvoiceLineDto>? Lines { get; set; }
}

/// <summary>
/// DTO para actualizar/crear líneas de detalle de una factura.
/// Las líneas se reemplazan completamente con el contenido enviado.
/// </summary>
public class UpdateInvoiceLineDto
{
    /// <summary>
    /// Identificador de la línea (opcional, usado solo para referencia).
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Número de línea dentro de la factura.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Descripción del concepto.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Precio unitario.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Divisa del precio unitario. Si no se informa, se usa la de la factura.
    /// </summary>
    public string? UnitPriceCurrency { get; set; }

    /// <summary>
    /// Tipo de IVA aplicado en la línea.
    /// </summary>
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// Total de la línea.
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Divisa del total de la línea. Si no se informa, se usa la de la factura.
    /// </summary>
    public string? LineTotalCurrency { get; set; }
}

