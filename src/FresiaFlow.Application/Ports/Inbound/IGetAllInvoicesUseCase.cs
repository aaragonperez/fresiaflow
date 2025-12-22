using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada (Inbound Port) para obtener todas las facturas recibidas.
/// Devuelve InvoiceReceived con todos los datos fiscales y de detalle.
/// </summary>
public interface IGetAllInvoicesUseCase
{
    /// <summary>
    /// Obtiene todas las facturas recibidas ordenadas por fecha de procesamiento descendente.
    /// </summary>
    Task<List<InvoiceReceived>> ExecuteAsync(CancellationToken cancellationToken = default);
}

