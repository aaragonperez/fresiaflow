using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida (Outbound Port) para persistencia de facturas.
/// El dominio define este contrato; la infraestructura lo implementa.
/// </summary>
public interface IInvoiceRepository
{
    Task<List<Invoice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetPendingInvoicesAsync(CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default);
    Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

