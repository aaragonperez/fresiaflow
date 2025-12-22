using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para eliminar facturas recibidas.
/// </summary>
public class DeleteInvoiceUseCase : IDeleteInvoiceUseCase
{
    private readonly IInvoiceReceivedRepository _invoiceReceivedRepository;

    public DeleteInvoiceUseCase(IInvoiceReceivedRepository invoiceReceivedRepository)
    {
        _invoiceReceivedRepository = invoiceReceivedRepository;
    }

    public async Task ExecuteAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceReceivedRepository.GetByIdAsync(invoiceId, cancellationToken);
        
        if (invoice == null)
            throw new InvalidOperationException($"Factura con ID {invoiceId} no encontrada.");

        await _invoiceReceivedRepository.DeleteAsync(invoiceId, cancellationToken);
    }
}

