using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Accounting;
using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para actualizar asientos contables.
/// </summary>
public class UpdateAccountingEntryUseCase : IUpdateAccountingEntryUseCase
{
    private readonly IAccountingEntryRepository _entryRepository;

    public UpdateAccountingEntryUseCase(IAccountingEntryRepository entryRepository)
    {
        _entryRepository = entryRepository;
    }

    public async Task<AccountingEntry> ExecuteAsync(
        Guid entryId,
        string? description = null,
        DateTime? entryDate = null,
        IEnumerable<AccountingEntryLineDto>? lines = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var entry = await _entryRepository.GetByIdAsync(entryId, cancellationToken);
        if (entry == null)
            throw new ArgumentException($"No se encontró el asiento con ID {entryId}", nameof(entryId));

        // Solo se pueden modificar asientos en estado Draft o Manual
        if (entry.Status == EntryStatus.Posted && entry.Source == EntrySource.Automatic)
            throw new InvalidOperationException("No se pueden modificar asientos automáticos ya contabilizados.");

        // Actualizar descripción
        if (description != null)
        {
            entry.UpdateDescription(description);
        }

        // Actualizar fecha
        if (entryDate.HasValue)
        {
            entry.UpdateEntryDate(entryDate.Value);
        }

        // Actualizar líneas
        if (lines != null)
        {
            var entryLines = new List<AccountingEntryLine>();
            
            foreach (var dto in lines)
            {
                AccountingEntryLine line;
                
                // Si tiene ID, intentar encontrar la línea existente y actualizarla
                if (dto.Id.HasValue)
                {
                    var existingLine = entry.Lines.FirstOrDefault(l => l.Id == dto.Id.Value);
                    if (existingLine != null)
                    {
                        // Actualizar línea existente
                        existingLine.UpdateAmount(new Money(dto.Amount, dto.Currency));
                        existingLine.UpdateDescription(dto.Description);
                        entryLines.Add(existingLine);
                        continue;
                    }
                }
                
                // Crear nueva línea (sin ID o ID no encontrado)
                line = new AccountingEntryLine(
                    accountingEntryId: entry.Id,
                    accountingAccountId: dto.AccountingAccountId,
                    side: dto.Side,
                    amount: new Money(dto.Amount, dto.Currency),
                    description: dto.Description);
                
                entryLines.Add(line);
            }

            entry.ReplaceLines(entryLines);
        }

        // Actualizar notas
        if (notes != null)
        {
            entry.UpdateNotes(notes);
        }

        // Validar que esté balanceado antes de guardar
        if (!entry.IsBalanced())
            throw new InvalidOperationException("El asiento no está balanceado. Debe = Haber.");

        await _entryRepository.UpdateAsync(entry, cancellationToken);
        return entry;
    }
}

