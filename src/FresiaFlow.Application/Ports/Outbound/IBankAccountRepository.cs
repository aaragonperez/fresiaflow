using FresiaFlow.Domain.Banking;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia de cuentas bancarias.
/// </summary>
public interface IBankAccountRepository
{
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<BankAccount>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<BankAccount> AddAsync(BankAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankAccount account, CancellationToken cancellationToken = default);
}

