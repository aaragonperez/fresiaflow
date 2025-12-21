using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Banking;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de cuentas bancarias usando EF Core.
/// </summary>
public class EfBankAccountRepository : IBankAccountRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfBankAccountRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<BankAccount>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankAccount> AddAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        await _context.BankAccounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task UpdateAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        _context.BankAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

