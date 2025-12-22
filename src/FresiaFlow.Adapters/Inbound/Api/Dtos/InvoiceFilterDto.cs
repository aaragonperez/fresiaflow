namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para filtros de facturas recibidas.
/// </summary>
public class InvoiceFilterDto
{
    /// <summary>
    /// Año fiscal (filtra por fecha de emisión).
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Trimestre fiscal (1-4). Requiere Year.
    /// </summary>
    public int? Quarter { get; set; }

    /// <summary>
    /// Nombre del proveedor (búsqueda parcial).
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// Tipo de pago: "Bank" o "Cash".
    /// </summary>
    public string? PaymentType { get; set; }
}

