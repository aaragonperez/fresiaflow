using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de facturas usando Entity Framework Core.
/// Adapter que conecta el dominio con la persistencia.
/// </summary>
public class EfInvoiceRepository : IInvoiceRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfInvoiceRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<List<Invoice>> GetPendingInvoicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Pending)
            .OrderBy(i => i.DueDate ?? i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Overdue ||
                       (i.Status == InvoiceStatus.Pending && i.DueDate.HasValue && i.DueDate.Value < now))
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetByIdAsync(id, cancellationToken);
        if (invoice != null)
        {
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

