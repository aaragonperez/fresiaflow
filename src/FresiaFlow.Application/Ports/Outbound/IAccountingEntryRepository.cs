using FresiaFlow.Domain.Accounting;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia de asientos contables.
/// </summary>
public interface IAccountingEntryRepository
{
    Task<AccountingEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountingEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    
    // Filtros por fecha
    Task<IEnumerable<AccountingEntry>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<AccountingEntry>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountingEntry>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    
    // Filtros por estado
    Task<IEnumerable<AccountingEntry>> GetByStatusAsync(EntryStatus status, CancellationToken cancellationToken = default);
    
    // Filtros por origen
    Task<IEnumerable<AccountingEntry>> GetBySourceAsync(EntrySource source, CancellationToken cancellationToken = default);
    
    // Filtros por factura
    Task<IEnumerable<AccountingEntry>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    
    // Métodos combinados
    Task<IEnumerable<AccountingEntry>> GetFilteredAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        EntryStatus? status = null,
        EntrySource? source = null,
        Guid? invoiceId = null,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(AccountingEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountingEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Verificar si ya existe un asiento para una factura
    Task<bool> ExistsForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene el siguiente número de asiento disponible para un año determinado.
    /// </summary>
    Task<int> GetNextEntryNumberAsync(int year, CancellationToken cancellationToken = default);
}

