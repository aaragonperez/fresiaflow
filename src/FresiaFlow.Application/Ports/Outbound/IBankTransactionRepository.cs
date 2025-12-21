using FresiaFlow.Domain.Banking;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia de transacciones bancarias.
/// </summary>
public interface IBankTransactionRepository
{
    Task<BankTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BankTransaction?> GetByExternalIdAsync(string externalTransactionId, CancellationToken cancellationToken = default);
    Task<List<BankTransaction>> GetUnreconciledAsync(CancellationToken cancellationToken = default);
    Task<List<BankTransaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<BankTransaction> AddAsync(BankTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankTransaction transaction, CancellationToken cancellationToken = default);
}

