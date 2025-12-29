using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Implementación del caso de uso para obtener facturas filtradas.
/// </summary>
public class GetFilteredInvoicesUseCase : IGetFilteredInvoicesUseCase
{
    private readonly IInvoiceReceivedRepository _repository;

    public GetFilteredInvoicesUseCase(IInvoiceReceivedRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<InvoiceReceived>> ExecuteAsync(
        int? year,
        int? quarter,
        string? supplierName,
        PaymentType? paymentType,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetFilteredAsync(
            year,
            quarter,
            supplierName,
            paymentType,
            cancellationToken);
    }
}

