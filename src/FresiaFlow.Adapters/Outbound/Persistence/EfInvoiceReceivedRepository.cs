using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de facturas recibidas con EF Core.
/// </summary>
public class EfInvoiceReceivedRepository : IInvoiceReceivedRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfInvoiceReceivedRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<InvoiceReceived?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.ReceivedDate)
            .ThenByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceReceived?> GetByInvoiceNumberAsync(
        string invoiceNumber, 
        CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetByYearAsync(
        int year, 
        CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.IssueDate.Year == year)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetByQuarterAsync(
        int year, 
        int quarter, 
        CancellationToken cancellationToken = default)
    {
        if (quarter < 1 || quarter > 4)
            throw new ArgumentException("El trimestre debe estar entre 1 y 4.", nameof(quarter));

        var startMonth = (quarter - 1) * 3 + 1;
        var endMonth = quarter * 3;
        var startDate = new DateTime(year, startMonth, 1);
        var endDate = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));

        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetBySupplierAsync(
        string supplierName, 
        CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.SupplierName.Contains(supplierName))
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetByExactSupplierNameAsync(
        string supplierName,
        CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.SupplierName == supplierName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetByPaymentTypeAsync(
        PaymentType paymentType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.PaymentType == paymentType)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InvoiceReceived>> GetFilteredAsync(
        int? year = null,
        int? quarter = null,
        string? supplierName = null,
        PaymentType? paymentType = null,
        CancellationToken cancellationToken = default)
    {
        // #region agent log
        try {
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                location = "EfInvoiceReceivedRepository.cs:104", 
                message = "GetFilteredAsync entry", 
                data = new { 
                    year = year?.ToString() ?? "null", 
                    quarter = quarter?.ToString() ?? "null", 
                    supplierName = supplierName ?? "null", 
                    paymentType = paymentType?.ToString() ?? "null",
                    yearHasValue = year.HasValue,
                    quarterHasValue = quarter.HasValue
                }, 
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                sessionId = "debug-session", 
                runId = "run1", 
                hypothesisId = "D" 
            }) + "\n";
            System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
        } catch (Exception) { /* Ignore log errors */ }
        // #endregion

        var query = _context.InvoicesReceived
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .AsQueryable();

        // Filtro por año y trimestre
        if (year.HasValue)
        {
            if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
            {
                // Filtro por trimestre específico del año
                var startMonth = (quarter.Value - 1) * 3 + 1;
                var endMonth = quarter.Value * 3;
                var startDate = new DateTime(year.Value, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = new DateTime(year.Value, endMonth, DateTime.DaysInMonth(year.Value, endMonth), 23, 59, 59, DateTimeKind.Utc);
                query = query.Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate);
                // #region agent log
                try {
                    var logEntry = System.Text.Json.JsonSerializer.Serialize(new { location = "EfInvoiceReceivedRepository.cs:126", message = "Applied year+quarter filter", data = new { year = year.Value, quarter = quarter.Value, startDate = startDate.ToString("yyyy-MM-dd HH:mm:ss UTC"), endDate = endDate.ToString("yyyy-MM-dd HH:mm:ss UTC"), startDateKind = startDate.Kind.ToString(), endDateKind = endDate.Kind.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "D" }) + "\n";
                    System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
                } catch (Exception) { /* Ignore log errors */ }
                // #endregion
            }
            else
            {
                // Solo filtro por año
                query = query.Where(i => i.IssueDate.Year == year.Value);
                // #region agent log
                try {
                    var logEntry = System.Text.Json.JsonSerializer.Serialize(new { location = "EfInvoiceReceivedRepository.cs:131", message = "Applied year-only filter", data = new { year = year.Value }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "D" }) + "\n";
                    System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
                } catch (Exception) { /* Ignore log errors */ }
                // #endregion
            }
        }
        else if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
        {
            // Si hay trimestre pero no año, usar el año actual
            var currentYear = DateTime.UtcNow.Year;
            var startMonth = (quarter.Value - 1) * 3 + 1;
            var endMonth = quarter.Value * 3;
            var startDate = new DateTime(currentYear, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(currentYear, endMonth, DateTime.DaysInMonth(currentYear, endMonth), 23, 59, 59, DateTimeKind.Utc);
            query = query.Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate);
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { location = "EfInvoiceReceivedRepository.cs:142", message = "Applied quarter-only filter", data = new { quarter = quarter.Value, currentYear, startDate = startDate.ToString("yyyy-MM-dd HH:mm:ss UTC"), endDate = endDate.ToString("yyyy-MM-dd HH:mm:ss UTC"), startDateKind = startDate.Kind.ToString(), endDateKind = endDate.Kind.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "D" }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
        }

        // Filtro por proveedor
        if (!string.IsNullOrWhiteSpace(supplierName))
        {
            query = query.Where(i => i.SupplierName.Contains(supplierName, StringComparison.OrdinalIgnoreCase));
            // #region agent log
            System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", 
                System.Text.Json.JsonSerializer.Serialize(new { location = "EfInvoiceReceivedRepository.cs:148", message = "Applied supplier filter", data = new { supplierName }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "D" }) + "\n");
            // #endregion
        }

        // Filtro por tipo de pago
        if (paymentType.HasValue)
        {
            // #region agent log - Debug antes de aplicar filtro
            try {
                // Obtener estadísticas antes del filtro (sin ejecutar la consulta completa)
                var statsQuery = _context.InvoicesReceived.AsQueryable();
                if (year.HasValue)
                {
                    if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
                    {
                        var startMonth = (quarter.Value - 1) * 3 + 1;
                        var endMonth = quarter.Value * 3;
                        var startDate = new DateTime(year.Value, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                        var endDate = new DateTime(year.Value, endMonth, DateTime.DaysInMonth(year.Value, endMonth), 23, 59, 59, DateTimeKind.Utc);
                        statsQuery = statsQuery.Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate);
                    }
                    else
                    {
                        statsQuery = statsQuery.Where(i => i.IssueDate.Year == year.Value);
                    }
                }
                else if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
                {
                    var currentYear = DateTime.UtcNow.Year;
                    var startMonth = (quarter.Value - 1) * 3 + 1;
                    var endMonth = quarter.Value * 3;
                    var startDate = new DateTime(currentYear, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                    var endDate = new DateTime(currentYear, endMonth, DateTime.DaysInMonth(currentYear, endMonth), 23, 59, 59, DateTimeKind.Utc);
                    statsQuery = statsQuery.Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate);
                }
                if (!string.IsNullOrWhiteSpace(supplierName))
                {
                    statsQuery = statsQuery.Where(i => i.SupplierName.Contains(supplierName, StringComparison.OrdinalIgnoreCase));
                }
                
                var stats = await statsQuery
                    .GroupBy(i => i.PaymentType)
                    .Select(g => new { paymentType = g.Key, count = g.Count() })
                    .ToListAsync(cancellationToken);
                
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "EfInvoiceReceivedRepository.cs:195", 
                    message = "Before paymentType filter - stats", 
                    data = new { 
                        requestedPaymentType = paymentType.Value.ToString(),
                        requestedPaymentTypeInt = (int)paymentType.Value,
                        invoicesByPaymentType = stats.Select(s => new { paymentType = s.paymentType.ToString(), paymentTypeInt = (int)s.paymentType, count = s.count }).ToList()
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "D" 
                }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
            
            query = query.Where(i => i.PaymentType == paymentType.Value);
            
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "EfInvoiceReceivedRepository.cs:240", 
                    message = "Applied paymentType filter", 
                    data = new { 
                        paymentType = paymentType.Value.ToString(),
                        paymentTypeInt = (int)paymentType.Value
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "D" 
                }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
        }

        // #region agent log
        try {
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                location = "EfInvoiceReceivedRepository.cs:199", 
                message = "Before executing query", 
                data = new { 
                    queryType = query.GetType().Name
                }, 
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                sessionId = "debug-session", 
                runId = "run1", 
                hypothesisId = "D" 
            }) + "\n";
            System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
        } catch (Exception) { /* Ignore log errors */ }
        // #endregion

        List<InvoiceReceived> result;
        try
        {
            result = await query
                .OrderByDescending(i => i.IssueDate)
                .ThenByDescending(i => i.ReceivedDate)
                .ToListAsync(cancellationToken);
            
            // #region agent log - Debug resultado final
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "EfInvoiceReceivedRepository.cs:298", 
                    message = "GetFilteredAsync final result", 
                    data = new { 
                        resultCount = result.Count,
                        resultPaymentTypes = result.GroupBy(i => i.PaymentType).Select(g => new { paymentType = g.Key.ToString(), paymentTypeInt = (int)g.Key, count = g.Count() }).ToList(),
                        sampleIds = result.Take(5).Select(i => new { id = i.Id.ToString(), paymentType = i.PaymentType.ToString(), paymentTypeInt = (int)i.PaymentType, hasPayments = i.Payments.Count > 0 }).ToList()
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "D" 
                }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "EfInvoiceReceivedRepository.cs:210", 
                    message = "Query execution error", 
                    data = new { 
                        errorMessage = ex.Message,
                        errorType = ex.GetType().Name,
                        stackTrace = ex.StackTrace
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "D" 
                }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
            throw;
        }
        
        // #region agent log
        try {
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                location = "EfInvoiceReceivedRepository.cs:225", 
                message = "GetFilteredAsync result", 
                data = new { resultCount = result.Count }, 
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                sessionId = "debug-session", 
                runId = "run1", 
                hypothesisId = "D" 
            }) + "\n";
            System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
        } catch (Exception) { /* Ignore log errors */ }
        // #endregion

        return result;
    }

    public async Task AddAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default)
    {
        await _context.InvoicesReceived.AddAsync(invoiceReceived, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InvoiceReceived invoiceReceived, CancellationToken cancellationToken = default)
    {
        _context.InvoicesReceived.Update(invoiceReceived);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateManyAsync(IEnumerable<InvoiceReceived> invoices, CancellationToken cancellationToken = default)
    {
        _context.InvoicesReceived.UpdateRange(invoices);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetByIdAsync(id, cancellationToken);
        if (invoice != null)
        {
            _context.InvoicesReceived.Remove(invoice);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
