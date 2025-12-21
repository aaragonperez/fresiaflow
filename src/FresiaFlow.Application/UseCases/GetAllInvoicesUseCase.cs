using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para obtener todas las facturas.
/// </summary>
public class GetAllInvoicesUseCase : IGetAllInvoicesUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetAllInvoicesUseCase(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<List<Invoice>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _invoiceRepository.GetAllAsync(cancellationToken);
    }
}

