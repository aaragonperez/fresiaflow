using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para obtener todas las facturas recibidas.
/// Devuelve InvoiceReceived que contiene todos los datos fiscales y de detalle.
/// </summary>
public class GetAllInvoicesUseCase : IGetAllInvoicesUseCase
{
    private readonly IInvoiceReceivedRepository _invoiceReceivedRepository;

    public GetAllInvoicesUseCase(IInvoiceReceivedRepository invoiceReceivedRepository)
    {
        _invoiceReceivedRepository = invoiceReceivedRepository;
    }

    public async Task<List<InvoiceReceived>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceReceivedRepository.GetAllAsync(cancellationToken);
        return invoices.ToList();
    }
}

