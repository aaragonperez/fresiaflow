using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

public class EfInvoiceProcessingSnapshotRepository : IInvoiceProcessingSnapshotRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfInvoiceProcessingSnapshotRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public Task<InvoiceProcessingSnapshot?> GetBySourceFilePathAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return _context.InvoiceProcessingSnapshots
            .FirstOrDefaultAsync(x => x.SourceFilePath == filePath, cancellationToken);
    }

    public Task<InvoiceProcessingSnapshot?> GetBySourceFileHashAsync(
        string fileHash,
        CancellationToken cancellationToken = default)
    {
        return _context.InvoiceProcessingSnapshots
            .FirstOrDefaultAsync(x => x.SourceFileHash == fileHash, cancellationToken);
    }

    public async Task AddAsync(
        InvoiceProcessingSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        await _context.InvoiceProcessingSnapshots.AddAsync(snapshot, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        InvoiceProcessingSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        _context.InvoiceProcessingSnapshots.Update(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

