using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Banking;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para sincronizar transacciones bancarias desde Open Banking.
/// </summary>
public class SyncBankTransactionsUseCase : ISyncBankTransactionsUseCase
{
    private readonly IBankProvider _bankProvider;
    private readonly IBankTransactionRepository _transactionRepository;
    private readonly IBankAccountRepository _accountRepository;

    public SyncBankTransactionsUseCase(
        IBankProvider bankProvider,
        IBankTransactionRepository transactionRepository,
        IBankAccountRepository accountRepository)
    {
        _bankProvider = bankProvider;
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
    }

    public async Task<SyncResult> ExecuteAsync(SyncBankTransactionsCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Obtener cuenta
        var account = await _accountRepository.GetByIdAsync(command.BankAccountId, cancellationToken);
        if (account == null)
            throw new InvalidOperationException($"Cuenta bancaria {command.BankAccountId} no encontrada.");

        if (string.IsNullOrEmpty(account.ExternalAccountId))
            throw new InvalidOperationException("La cuenta no tiene un ID externo configurado.");

        // 2. Determinar rango de fechas
        var fromDate = command.FromDate ?? account.LastSyncAt.AddDays(-7);
        var toDate = command.ToDate ?? DateTime.UtcNow;

        // 3. Obtener transacciones del proveedor
        var externalTransactions = await _bankProvider.GetTransactionsAsync(
            account.ExternalAccountId,
            fromDate,
            toDate,
            cancellationToken);

        // 4. Sincronizar con base de datos
        int newCount = 0;
        int updatedCount = 0;

        foreach (var externalTransaction in externalTransactions)
        {
            if (string.IsNullOrEmpty(externalTransaction.ExternalTransactionId))
                continue;

            var existing = await _transactionRepository.GetByExternalIdAsync(
                externalTransaction.ExternalTransactionId,
                cancellationToken);

            if (existing == null)
            {
                // Crear nueva transacción con el accountId correcto
                var newTransaction = new BankTransaction(
                    account.Id,
                    externalTransaction.TransactionDate,
                    externalTransaction.Amount,
                    externalTransaction.Description,
                    externalTransaction.Reference,
                    externalTransaction.ExternalTransactionId);

                await _transactionRepository.AddAsync(newTransaction, cancellationToken);
                newCount++;
            }
            else
            {
                // Actualizar si es necesario
                await _transactionRepository.UpdateAsync(existing, cancellationToken);
                updatedCount++;
            }
        }

        // 5. Actualizar última sincronización
        account.MarkSyncCompleted();
        await _accountRepository.UpdateAsync(account, cancellationToken);

        return new SyncResult(newCount, updatedCount, DateTime.UtcNow);
    }
}

