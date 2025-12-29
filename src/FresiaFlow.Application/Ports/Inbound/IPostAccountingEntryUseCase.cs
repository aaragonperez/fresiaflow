using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para contabilizar (post) un asiento.
/// </summary>
public interface IPostAccountingEntryUseCase
{
    Task ExecuteAsync(Guid entryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Contabiliza todos los asientos balanceados en estado Draft.
    /// </summary>
    Task<PostBalancedEntriesResult> PostAllBalancedEntriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de contabilizar asientos balanceados.
/// </summary>
public record PostBalancedEntriesResult(
    int TotalProcessed,
    int SuccessCount,
    int ErrorCount,
    List<string> Errors);

