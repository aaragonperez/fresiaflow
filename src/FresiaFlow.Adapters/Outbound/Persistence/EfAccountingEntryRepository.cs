using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Accounting;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de asientos contables con EF Core.
/// </summary>
public class EfAccountingEntryRepository : IAccountingEntryRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfAccountingEntryRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<AccountingEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .OrderByDescending(e => e.EntryYear)
            .ThenByDescending(e => e.EntryNumber ?? 0)
            .ThenByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .Where(e => e.EntryDate.Year == year)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetByMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .Where(e => e.EntryDate.Year == year && e.EntryDate.Month == month)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetByStatusAsync(
        EntryStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetBySourceAsync(
        EntrySource source,
        CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .Where(e => e.Source == source)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .Include(e => e.Lines)
            .Where(e => e.InvoiceId == invoiceId)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountingEntry>> GetFilteredAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        EntryStatus? status = null,
        EntrySource? source = null,
        Guid? invoiceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AccountingEntries
            .Include(e => e.Lines)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(e => e.EntryDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EntryDate <= endDate.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(e => e.Source == source.Value);
        }

        if (invoiceId.HasValue)
        {
            query = query.Where(e => e.InvoiceId == invoiceId.Value);
        }

        return await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AccountingEntry entry, CancellationToken cancellationToken = default)
    {
        // #region agent log
        try {
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                location = "EfAccountingEntryRepository.cs:152", 
                message = "AddAsync entry", 
                data = new { 
                    entryId = entry.Id.ToString(),
                    entryNumber = entry.EntryNumber,
                    entryYear = entry.EntryYear,
                    invoiceId = entry.InvoiceId?.ToString(),
                    linesCount = entry.Lines.Count(),
                    totalDebit = entry.GetTotalDebit().Value,
                    totalCredit = entry.GetTotalCredit().Value
                }, 
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                sessionId = "debug-session", 
                runId = "run1", 
                hypothesisId = "E" 
            }) + "\n";
            System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
        } catch (Exception) { /* Ignore log errors */ }
        // #endregion
        
        await _context.AccountingEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        // #region agent log
        try {
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                location = "EfAccountingEntryRepository.cs:155", 
                message = "AddAsync SaveChanges completed", 
                data = new { 
                    entryId = entry.Id.ToString(),
                    entryNumber = entry.EntryNumber
                }, 
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                sessionId = "debug-session", 
                runId = "run1", 
                hypothesisId = "E" 
            }) + "\n";
            System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
        } catch (Exception) { /* Ignore log errors */ }
        // #endregion
    }

    public async Task UpdateAsync(AccountingEntry entry, CancellationToken cancellationToken = default)
    {
        // Cargar la entidad existente con tracking para poder actualizarla
        var trackedEntry = await _context.AccountingEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == entry.Id, cancellationToken);

        if (trackedEntry == null)
            throw new InvalidOperationException($"No se encontró el asiento con ID {entry.Id}");

        bool hasChanges = false;

        // Actualizar propiedades del asiento usando los métodos del dominio
        if (trackedEntry.Description != entry.Description)
        {
            trackedEntry.UpdateDescription(entry.Description);
            hasChanges = true;
        }
        if (trackedEntry.EntryDate != entry.EntryDate)
        {
            trackedEntry.UpdateEntryDate(entry.EntryDate);
            hasChanges = true;
        }
        if (trackedEntry.Notes != entry.Notes)
        {
            trackedEntry.UpdateNotes(entry.Notes);
            hasChanges = true;
        }

        // Identificar líneas a mantener, actualizar, eliminar y agregar
        var newLineIds = entry.Lines.Select(l => l.Id).ToHashSet();
        var existingLineIds = trackedEntry.Lines.Select(l => l.Id).ToHashSet();
        
        // Eliminar líneas que ya no existen en la nueva lista
        var linesToRemove = trackedEntry.Lines.Where(l => !newLineIds.Contains(l.Id)).ToList();
        if (linesToRemove.Any())
        {
            foreach (var line in linesToRemove)
            {
                _context.AccountingEntryLines.Remove(line);
            }
            hasChanges = true;
        }

        // Actualizar o agregar líneas
        foreach (var newLine in entry.Lines)
        {
            var existingLine = trackedEntry.Lines.FirstOrDefault(l => l.Id == newLine.Id);
            if (existingLine != null)
            {
                // Verificar si hay cambios en la línea existente
                bool lineChanged = false;
                
                if (existingLine.Amount.Value != newLine.Amount.Value || 
                    existingLine.Amount.Currency != newLine.Amount.Currency)
                {
                    existingLine.UpdateAmount(newLine.Amount);
                    lineChanged = true;
                }
                
                if (existingLine.Description != newLine.Description)
                {
                    existingLine.UpdateDescription(newLine.Description);
                    lineChanged = true;
                }
                
                if (lineChanged)
                {
                    hasChanges = true;
                }
            }
            else
            {
                // Agregar nueva línea
                _context.AccountingEntryLines.Add(newLine);
                hasChanges = true;
            }
        }

        // Solo guardar si hay cambios reales
        if (hasChanges || _context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await GetByIdAsync(id, cancellationToken);
        if (entry != null)
        {
            _context.AccountingEntries.Remove(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingEntries
            .AnyAsync(e => e.InvoiceId == invoiceId, cancellationToken);
    }

    public async Task<int> GetNextEntryNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        // Obtener el máximo número de asiento para el año, o 0 si no hay ninguno
        var maxNumber = await _context.AccountingEntries
            .Where(e => e.EntryYear == year && e.EntryNumber.HasValue)
            .Select(e => e.EntryNumber!.Value)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        // Si no hay asientos para este año, maxNumber será 0 (default de int)
        // Si hay asientos, maxNumber será el máximo
        return maxNumber + 1;
    }
}

