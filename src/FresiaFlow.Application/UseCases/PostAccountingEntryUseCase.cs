using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para contabilizar (post) un asiento.
/// </summary>
public class PostAccountingEntryUseCase : IPostAccountingEntryUseCase
{
    private readonly IAccountingEntryRepository _entryRepository;

    public PostAccountingEntryUseCase(IAccountingEntryRepository entryRepository)
    {
        _entryRepository = entryRepository;
    }

    public async Task ExecuteAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await _entryRepository.GetByIdAsync(entryId, cancellationToken);
        if (entry == null)
            throw new ArgumentException($"No se encontró el asiento con ID {entryId}", nameof(entryId));

        // Si el asiento ya tiene número, usarlo. Si no, obtener el siguiente número
        int entryNumber;
        if (entry.EntryNumber.HasValue)
        {
            entryNumber = entry.EntryNumber.Value;
        }
        else
        {
            entryNumber = await _entryRepository.GetNextEntryNumberAsync(entry.EntryYear, cancellationToken);
        }
        
        entry.Post(entryNumber);
        await _entryRepository.UpdateAsync(entry, cancellationToken);
    }

    public async Task<PostBalancedEntriesResult> PostAllBalancedEntriesAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        int successCount = 0;
        int errorCount = 0;

        // Obtener todos los asientos en estado Draft
        var draftEntries = await _entryRepository.GetByStatusAsync(EntryStatus.Draft, cancellationToken);
        
        // Filtrar solo los balanceados
        var balancedEntries = draftEntries.Where(e => e.IsBalanced()).ToList();
        
        // Agrupar por año para asignar números correlativos correctamente
        var entriesByYear = balancedEntries.GroupBy(e => e.EntryYear).ToList();

        foreach (var yearGroup in entriesByYear)
        {
            var year = yearGroup.Key;
            var entriesForYear = yearGroup.OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAt).ToList();
            
            // Obtener el siguiente número disponible para este año
            var nextNumber = await _entryRepository.GetNextEntryNumberAsync(year, cancellationToken);
            
            foreach (var entry in entriesForYear)
            {
                try
                {
                    // Si el asiento ya tiene número, usarlo. Si no, usar el siguiente
                    int entryNumber;
                    if (entry.EntryNumber.HasValue)
                    {
                        entryNumber = entry.EntryNumber.Value;
                    }
                    else
                    {
                        entryNumber = nextNumber;
                        nextNumber++; // Incrementar para el siguiente asiento del mismo año
                    }
                    
                    entry.Post(entryNumber);
                    await _entryRepository.UpdateAsync(entry, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add($"Error contabilizando asiento {entry.Id}: {ex.Message}");
                }
            }
        }

        return new PostBalancedEntriesResult(
            TotalProcessed: balancedEntries.Count,
            SuccessCount: successCount,
            ErrorCount: errorCount,
            Errors: errors);
    }
}

