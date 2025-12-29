using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Accounting;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio del plan de cuentas con EF Core.
/// </summary>
public class EfAccountingAccountRepository : IAccountingAccountRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfAccountingAccountRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<AccountingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingAccounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<AccountingAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingAccounts
            .FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<AccountingAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccountingAccounts
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingAccount>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccountingAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingAccount>> GetByTypeAsync(
        AccountType type,
        CancellationToken cancellationToken = default)
    {
        return await _context.AccountingAccounts
            .Where(a => a.Type == type && a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AccountingAccount account, CancellationToken cancellationToken = default)
    {
        await _context.AccountingAccounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AccountingAccount account, CancellationToken cancellationToken = default)
    {
        _context.AccountingAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await GetByIdAsync(id, cancellationToken);
        if (account != null)
        {
            _context.AccountingAccounts.Remove(account);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

