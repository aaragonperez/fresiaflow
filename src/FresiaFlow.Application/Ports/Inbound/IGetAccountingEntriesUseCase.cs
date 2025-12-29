using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para obtener asientos contables.
/// </summary>
public interface IGetAccountingEntriesUseCase
{
    Task<IEnumerable<AccountingEntry>> ExecuteAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        EntryStatus? status = null,
        EntrySource? source = null,
        Guid? invoiceId = null,
        CancellationToken cancellationToken = default);
}

