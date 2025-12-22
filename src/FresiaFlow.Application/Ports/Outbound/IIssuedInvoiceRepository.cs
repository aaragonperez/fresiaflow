using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia de facturas emitidas.
/// </summary>
public interface IIssuedInvoiceRepository
{
    Task<List<IssuedInvoice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IssuedInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IssuedInvoice?> GetByInvoiceNumberAsync(string series, string invoiceNumber, CancellationToken cancellationToken = default);
    Task<List<IssuedInvoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IssuedInvoice> AddAsync(IssuedInvoice invoice, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<IssuedInvoice> invoices, CancellationToken cancellationToken = default);
    Task UpdateAsync(IssuedInvoice invoice, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

