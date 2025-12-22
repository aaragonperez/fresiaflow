namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para exportar facturas emitidas.
/// </summary>
public class ExportIssuedInvoicesDto
{
    /// <summary>
    /// Año de las facturas a exportar.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Trimestre (1-4). Requiere Year.
    /// </summary>
    public int? Quarter { get; set; }

    /// <summary>
    /// Mes (1-12). Requiere Year.
    /// </summary>
    public int? Month { get; set; }

    /// <summary>
    /// Fecha de inicio del rango. Si se proporciona, requiere EndDate.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Fecha de fin del rango. Si se proporciona, requiere StartDate.
    /// </summary>
    public DateTime? EndDate { get; set; }
}

