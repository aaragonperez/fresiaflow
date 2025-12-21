using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia de facturas recibidas.
/// </summary>
public interface IInvoiceReceivedRepository
{
    Task<InvoiceReceived?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvoiceReceived>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<InvoiceReceived>> GetByStatusAsync(InvoiceReceivedStatus status, CancellationToken cancellationToken = default);
    Task<InvoiceReceived?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task AddAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default);
    Task UpdateAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default);
}

