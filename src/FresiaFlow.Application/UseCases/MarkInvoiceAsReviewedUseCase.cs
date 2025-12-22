using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para marcar una factura recibida como revisada.
/// </summary>
public class MarkInvoiceAsReviewedUseCase : IMarkInvoiceAsReviewedUseCase
{
    private readonly IInvoiceReceivedRepository _repository;

    public MarkInvoiceAsReviewedUseCase(IInvoiceReceivedRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        // NOTA: Este caso de uso ya no tiene sentido en el modelo contable.
        // Las facturas recibidas están contabilizadas desde su recepción.
        // Se mantiene para compatibilidad pero no hace nada.
        var invoice = await _repository.GetByIdAsync(invoiceId, cancellationToken);

        if (invoice == null)
            throw new InvalidOperationException($"Factura con ID {invoiceId} no encontrada.");

        // No hay nada que hacer - la factura ya está contabilizada
    }
}

