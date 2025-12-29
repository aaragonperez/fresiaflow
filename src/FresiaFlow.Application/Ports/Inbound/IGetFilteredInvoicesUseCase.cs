using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Caso de uso para obtener facturas recibidas con filtros.
/// </summary>
public interface IGetFilteredInvoicesUseCase
{
    Task<IEnumerable<InvoiceReceived>> ExecuteAsync(
        int? year,
        int? quarter,
        string? supplierName,
        PaymentType? paymentType,
        CancellationToken cancellationToken = default);
}

