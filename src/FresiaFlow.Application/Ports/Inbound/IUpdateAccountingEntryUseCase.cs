using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para actualizar asientos contables.
/// </summary>
public interface IUpdateAccountingEntryUseCase
{
    Task<AccountingEntry> ExecuteAsync(
        Guid entryId,
        string? description = null,
        DateTime? entryDate = null,
        IEnumerable<AccountingEntryLineDto>? lines = null,
        string? notes = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO para crear/actualizar líneas de asiento.
/// </summary>
public record AccountingEntryLineDto(
    Guid AccountingAccountId,
    EntrySide Side,
    decimal Amount,
    string Currency,
    string? Description = null,
    Guid? Id = null); // ID opcional para preservar líneas existentes

