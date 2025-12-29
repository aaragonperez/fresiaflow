using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para obtener asientos contables con filtros.
/// </summary>
public class GetAccountingEntriesUseCase : IGetAccountingEntriesUseCase
{
    private readonly IAccountingEntryRepository _entryRepository;

    public GetAccountingEntriesUseCase(IAccountingEntryRepository entryRepository)
    {
        _entryRepository = entryRepository;
    }

    public async Task<IEnumerable<AccountingEntry>> ExecuteAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        EntryStatus? status = null,
        EntrySource? source = null,
        Guid? invoiceId = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await _entryRepository.GetFilteredAsync(
            startDate: startDate,
            endDate: endDate,
            status: status,
            source: source,
            invoiceId: invoiceId,
            cancellationToken: cancellationToken);

        return entries.OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.CreatedAt);
    }
}

