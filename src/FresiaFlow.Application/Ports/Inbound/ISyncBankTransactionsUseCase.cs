using FresiaFlow.Domain.Banking;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para sincronizar transacciones bancarias.
/// </summary>
public interface ISyncBankTransactionsUseCase
{
    /// <summary>
    /// Sincroniza las transacciones bancarias desde el proveedor Open Banking.
    /// </summary>
    Task<SyncResult> ExecuteAsync(SyncBankTransactionsCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Comando para sincronizar transacciones bancarias.
/// </summary>
public record SyncBankTransactionsCommand(
    Guid BankAccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);

/// <summary>
/// Resultado de la sincronización.
/// </summary>
public record SyncResult(
    int NewTransactionsCount,
    int UpdatedTransactionsCount,
    DateTime LastSyncDate
);

