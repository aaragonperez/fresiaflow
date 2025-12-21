using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada (Inbound Port) para obtener todas las facturas.
/// </summary>
public interface IGetAllInvoicesUseCase
{
    /// <summary>
    /// Obtiene todas las facturas ordenadas por fecha de emisión descendente.
    /// </summary>
    Task<List<Invoice>> ExecuteAsync(CancellationToken cancellationToken = default);
}

