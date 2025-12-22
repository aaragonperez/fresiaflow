using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de facturas emitidas usando EF Core.
/// </summary>
public class EfIssuedInvoiceRepository : IIssuedInvoiceRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfIssuedInvoiceRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<IssuedInvoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.IssuedInvoices
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IssuedInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.IssuedInvoices
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IssuedInvoice?> GetByInvoiceNumberAsync(string series, string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.IssuedInvoices
            .FirstOrDefaultAsync(i => i.Series == series && i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<List<IssuedInvoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.IssuedInvoices
            .Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IssuedInvoice> AddAsync(IssuedInvoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.IssuedInvoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task AddRangeAsync(IEnumerable<IssuedInvoice> invoices, CancellationToken cancellationToken = default)
    {
        await _context.IssuedInvoices.AddRangeAsync(invoices, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(IssuedInvoice invoice, CancellationToken cancellationToken = default)
    {
        _context.IssuedInvoices.Update(invoice);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetByIdAsync(id, cancellationToken);
        if (invoice != null)
        {
            _context.IssuedInvoices.Remove(invoice);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

