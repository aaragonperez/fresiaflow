using FresiaFlow.Application.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada (Inbound Port) para procesar facturas entrantes.
/// </summary>
public interface IProcessIncomingInvoiceCommandHandler
{
    /// <summary>
    /// Procesa una factura entrante desde un archivo PDF.
    /// </summary>
    Task<Guid> HandleAsync(
        ProcessIncomingInvoiceCommand command,
        CancellationToken cancellationToken = default);
}

