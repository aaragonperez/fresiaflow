using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia del plan de cuentas.
/// </summary>
public interface IAccountingAccountRepository
{
    Task<AccountingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountingAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountingAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountingAccount>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountingAccount>> GetByTypeAsync(AccountType type, CancellationToken cancellationToken = default);
    
    Task AddAsync(AccountingAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountingAccount account, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

