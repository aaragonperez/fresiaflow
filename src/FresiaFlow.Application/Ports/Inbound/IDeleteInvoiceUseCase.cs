namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para eliminar facturas recibidas.
/// </summary>
public interface IDeleteInvoiceUseCase
{
    /// <summary>
    /// Elimina una factura recibida por su ID.
    /// </summary>
    Task ExecuteAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}

