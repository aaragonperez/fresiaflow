using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia de facturas recibidas.
/// </summary>
public interface IInvoiceReceivedRepository
{
    Task<InvoiceReceived?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvoiceReceived>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InvoiceReceived?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    
    // Filtros contables
    Task<IEnumerable<InvoiceReceived>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvoiceReceived>> GetByQuarterAsync(int year, int quarter, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvoiceReceived>> GetBySupplierAsync(string supplierName, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvoiceReceived>> GetByPaymentTypeAsync(PaymentType paymentType, CancellationToken cancellationToken = default);
    
    // Métodos combinados para filtros complejos
    Task<IEnumerable<InvoiceReceived>> GetFilteredAsync(
        int? year = null,
        int? quarter = null,
        string? supplierName = null,
        PaymentType? paymentType = null,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default);
    Task UpdateAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
