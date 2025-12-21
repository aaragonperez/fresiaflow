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
        var invoice = await _repository.GetByIdAsync(invoiceId, cancellationToken);

        if (invoice == null)
            throw new InvalidOperationException($"Factura con ID {invoiceId} no encontrada.");

        invoice.MarkAsReviewed();
        await _repository.UpdateAsync(invoice, cancellationToken);
    }
}

