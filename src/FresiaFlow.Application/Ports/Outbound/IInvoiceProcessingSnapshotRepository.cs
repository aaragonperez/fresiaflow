using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto para cachear cada paso del pipeline de procesamiento.
/// </summary>
public interface IInvoiceProcessingSnapshotRepository
{
    Task<InvoiceProcessingSnapshot?> GetBySourceFilePathAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<InvoiceProcessingSnapshot?> GetBySourceFileHashAsync(
        string fileHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(InvoiceProcessingSnapshot snapshot, CancellationToken cancellationToken = default);

    Task UpdateAsync(InvoiceProcessingSnapshot snapshot, CancellationToken cancellationToken = default);
}

