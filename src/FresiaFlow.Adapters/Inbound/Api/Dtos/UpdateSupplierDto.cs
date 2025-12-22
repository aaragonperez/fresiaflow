namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para actualizar el nombre del proveedor de una factura.
/// </summary>
public class UpdateSupplierDto
{
    /// <summary>
    /// Nuevo nombre del proveedor.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;
}

