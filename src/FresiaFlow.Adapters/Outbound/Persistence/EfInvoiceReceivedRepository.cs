using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de facturas recibidas con EF Core.
/// </summary>
public class EfInvoiceReceivedRepository : IInvoiceReceivedRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfInvoiceReceivedRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<InvoiceReceived?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .OrderByDescending(i => i.ProcessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetByStatusAsync(
        InvoiceReceivedStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.ProcessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceReceived?> GetByInvoiceNumberAsync(
        string invoiceNumber, 
        CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task AddAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default)
    {
        await _context.InvoicesReceived.AddAsync(invoiceReceived, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default)
    {
        _context.InvoicesReceived.Update(invoiceReceived);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

