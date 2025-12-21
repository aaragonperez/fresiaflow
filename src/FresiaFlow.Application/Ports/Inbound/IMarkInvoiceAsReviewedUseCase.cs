namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada (Inbound Port) para marcar una factura recibida como revisada.
/// </summary>
public interface IMarkInvoiceAsReviewedUseCase
{
    /// <summary>
    /// Marca una factura recibida como revisada.
    /// </summary>
    Task ExecuteAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}

