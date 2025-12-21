using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Banking;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de transacciones bancarias usando EF Core.
/// </summary>
public class EfBankTransactionRepository : IBankTransactionRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfBankTransactionRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<BankTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BankTransactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<BankTransaction?> GetByExternalIdAsync(string externalTransactionId, CancellationToken cancellationToken = default)
    {
        return await _context.BankTransactions
            .FirstOrDefaultAsync(t => t.ExternalTransactionId == externalTransactionId, cancellationToken);
    }

    public async Task<List<BankTransaction>> GetUnreconciledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BankTransactions
            .Where(t => !t.IsReconciled)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BankTransaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.BankTransactions
            .Where(t => t.BankAccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankTransaction> AddAsync(BankTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.BankTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task UpdateAsync(BankTransaction transaction, CancellationToken cancellationToken = default)
    {
        _context.BankTransactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

