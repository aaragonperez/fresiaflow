using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Accounting;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para generar asientos contables automáticamente desde facturas recibidas.
/// </summary>
public class GenerateAccountingEntriesUseCase : IGenerateAccountingEntriesUseCase
{
    private readonly IInvoiceReceivedRepository _invoiceRepository;
    private readonly IAccountingEntryRepository _entryRepository;
    private readonly IAccountingAccountRepository _accountRepository;
    private readonly IAccountingProgressNotifier? _progressNotifier;
    private readonly ILogger<GenerateAccountingEntriesUseCase>? _logger;

    // Códigos de cuentas estándar del Plan General Contable español
    private const string ACCOUNT_SUPPLIERS = "400"; // Proveedores
    private const string ACCOUNT_EXPENSES = "600"; // Compras
    private const string ACCOUNT_VAT_PAYABLE = "472"; // H.P. IVA soportado
    private const string ACCOUNT_IRPF_PAYABLE = "4751"; // H.P. acreedora por retenciones practicadas
    private const string ACCOUNT_BANK = "572"; // Bancos
    private const string ACCOUNT_CASH = "570"; // Caja

    public GenerateAccountingEntriesUseCase(
        IInvoiceReceivedRepository invoiceRepository,
        IAccountingEntryRepository entryRepository,
        IAccountingAccountRepository accountRepository,
        IAccountingProgressNotifier? progressNotifier = null,
        ILogger<GenerateAccountingEntriesUseCase>? logger = null)
    {
        _invoiceRepository = invoiceRepository;
        _entryRepository = entryRepository;
        _accountRepository = accountRepository;
        _progressNotifier = progressNotifier;
        _logger = logger;
    }

    public async Task<GenerateAccountingEntriesResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var failedInvoices = new List<FailedInvoiceInfo>();
        int successCount = 0;
        int errorCount = 0;

        // Obtener todas las facturas
        var invoices = await _invoiceRepository.GetAllAsync(cancellationToken);
        var totalInvoices = invoices.Count();
        var processedCount = 0;

        _logger?.LogInformation("Iniciando generación de asientos. Total facturas: {TotalInvoices}", totalInvoices);

        // Notificar inicio
        if (_progressNotifier != null)
        {
            await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
            {
                ProcessedCount = 0,
                TotalCount = totalInvoices,
                Percentage = 0,
                Status = "generating",
                Message = "Iniciando generación de asientos contables...",
                SuccessCount = 0,
                ErrorCount = 0
            }, cancellationToken);
        }

        foreach (var invoice in invoices)
        {
            processedCount++;
            var percentage = totalInvoices > 0 ? (int)((processedCount * 100.0) / totalInvoices) : 0;

            // Notificar progreso
            if (_progressNotifier != null)
            {
                await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
                {
                    ProcessedCount = processedCount,
                    TotalCount = totalInvoices,
                    Percentage = percentage,
                    Status = "generating",
                    Message = $"Procesando factura {processedCount} de {totalInvoices}...",
                    CurrentInvoiceNumber = invoice.InvoiceNumber,
                    CurrentInvoiceSupplier = invoice.SupplierName,
                    SuccessCount = successCount,
                    ErrorCount = errorCount
                }, cancellationToken);
            }

            try
            {
                // Verificar si ya existe un asiento para esta factura
                var exists = await _entryRepository.ExistsForInvoiceAsync(invoice.Id, cancellationToken);
                
                if (exists)
                {
                    // Notificar que se omitió
                    if (_progressNotifier != null)
                    {
                        await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
                        {
                            ProcessedCount = processedCount,
                            TotalCount = totalInvoices,
                            Percentage = percentage,
                            Status = "generating",
                            Message = $"Factura {invoice.InvoiceNumber} ya tiene asiento, omitiendo...",
                            CurrentInvoiceNumber = invoice.InvoiceNumber,
                            CurrentInvoiceSupplier = invoice.SupplierName,
                            SuccessCount = successCount,
                            ErrorCount = errorCount
                        }, cancellationToken);
                    }
                    continue;
                }

                // Generar asiento para esta factura
                var (entry, failureReason) = await GenerateEntryForInvoiceAsync(invoice, cancellationToken);
                
                if (entry != null)
                {
                    await _entryRepository.AddAsync(entry, cancellationToken);
                    successCount++;
                    _logger?.LogDebug("Asiento generado exitosamente para factura {InvoiceNumber} (ID: {InvoiceId})", invoice.InvoiceNumber, invoice.Id);
                }
                else
                {
                    _logger?.LogWarning("No se pudo generar asiento para factura {InvoiceNumber} (ID: {InvoiceId}): {Reason}", 
                        invoice.InvoiceNumber, invoice.Id, failureReason ?? "Razón desconocida");
                    errorCount++;
                    var errorMsg = $"No se pudo generar asiento para factura {invoice.InvoiceNumber}: {failureReason ?? "Razón desconocida"}";
                    errors.Add(errorMsg);
                    failedInvoices.Add(new FailedInvoiceInfo(
                        invoice.Id,
                        invoice.InvoiceNumber ?? "Sin número",
                        invoice.SupplierName ?? "Sin proveedor",
                        failureReason ?? "Razón desconocida"));
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                var errorMessage = $"Error generando asiento para factura {invoice.InvoiceNumber}: {ex.Message}";
                errors.Add(errorMessage);
                failedInvoices.Add(new FailedInvoiceInfo(
                    invoice.Id,
                    invoice.InvoiceNumber ?? "Sin número",
                    invoice.SupplierName ?? "Sin proveedor",
                    ex.Message));

                // Notificar error
                if (_progressNotifier != null)
                {
                    await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
                    {
                        ProcessedCount = processedCount,
                        TotalCount = totalInvoices,
                        Percentage = percentage,
                        Status = "generating",
                        Message = errorMessage,
                        CurrentInvoiceNumber = invoice.InvoiceNumber,
                        CurrentInvoiceSupplier = invoice.SupplierName,
                        SuccessCount = successCount,
                        ErrorCount = errorCount,
                        CurrentError = ex.Message
                    }, cancellationToken);
                }
            }
        }

        // Notificar finalización
        if (_progressNotifier != null)
        {
            await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
            {
                ProcessedCount = totalInvoices,
                TotalCount = totalInvoices,
                Percentage = 100,
                Status = "completed",
                Message = $"Generación completada: {successCount} asientos generados, {errorCount} errores",
                SuccessCount = successCount,
                ErrorCount = errorCount
            }, cancellationToken);
        }

        return new GenerateAccountingEntriesResult(
            TotalProcessed: totalInvoices,
            SuccessCount: successCount,
            ErrorCount: errorCount,
            Errors: errors,
            FailedInvoices: failedInvoices);
    }

    public async Task<AccountingEntry?> GenerateForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice == null)
            return null;

        // Verificar si ya existe un asiento
        var exists = await _entryRepository.ExistsForInvoiceAsync(invoiceId, cancellationToken);
        if (exists)
            return null;

        var (entry, _) = await GenerateEntryForInvoiceAsync(invoice, cancellationToken);
        if (entry != null)
        {
            await _entryRepository.AddAsync(entry, cancellationToken);
        }

        return entry;
    }

    /// <summary>
    /// Regenera un asiento eliminando el existente y creando uno nuevo.
    /// </summary>
    public async Task<AccountingEntry?> RegenerateForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                _logger?.LogWarning("No se encontró la factura {InvoiceId} para regenerar asiento", invoiceId);
                return null;
            }

            // Buscar y eliminar el asiento existente si existe
            var existingEntries = await _entryRepository.GetByInvoiceIdAsync(invoiceId, cancellationToken);
            foreach (var existingEntry in existingEntries)
            {
                // Solo eliminar si no está contabilizado
                if (existingEntry.Status == EntryStatus.Draft)
                {
                    try
                    {
                        await _entryRepository.DeleteAsync(existingEntry.Id, cancellationToken);
                        _logger?.LogDebug("Asiento {EntryId} eliminado para regeneración de factura {InvoiceId}", existingEntry.Id, invoiceId);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error eliminando asiento {EntryId} para factura {InvoiceId}", existingEntry.Id, invoiceId);
                        // Continuar con la regeneración aunque falle la eliminación
                    }
                }
                else
                {
                    _logger?.LogWarning("No se puede regenerar asiento {EntryId} para factura {InvoiceId} porque está en estado {Status}", 
                        existingEntry.Id, invoiceId, existingEntry.Status);
                }
            }

            // Generar nuevo asiento
            var (entry, _) = await GenerateEntryForInvoiceAsync(invoice, cancellationToken);
            if (entry != null)
            {
                await _entryRepository.AddAsync(entry, cancellationToken);
                _logger?.LogDebug("Asiento {EntryId} regenerado exitosamente para factura {InvoiceId}", entry.Id, invoiceId);
            }
            else
            {
                _logger?.LogWarning("No se pudo generar asiento para factura {InvoiceId}", invoiceId);
            }

            return entry;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error regenerando asiento para factura {InvoiceId}", invoiceId);
            throw;
        }
    }

    /// <summary>
    /// Regenera asientos para múltiples facturas.
    /// </summary>
    public async Task<GenerateAccountingEntriesResult> RegenerateForInvoicesAsync(
        IEnumerable<Guid> invoiceIds, 
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        int successCount = 0;
        int errorCount = 0;
        var invoiceIdsList = invoiceIds.ToList();
        var totalInvoices = invoiceIdsList.Count;
        var processedCount = 0;

        // Notificar inicio
        if (_progressNotifier != null)
        {
            await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
            {
                ProcessedCount = 0,
                TotalCount = totalInvoices,
                Percentage = 0,
                Status = "generating",
                Message = "Iniciando regeneración de asientos contables...",
                SuccessCount = 0,
                ErrorCount = 0
            }, cancellationToken);
        }

        foreach (var invoiceId in invoiceIdsList)
        {
            // Verificar si se canceló la operación
            cancellationToken.ThrowIfCancellationRequested();

            processedCount++;
            var percentage = totalInvoices > 0 ? (int)((processedCount * 100.0) / totalInvoices) : 0;

            // Obtener información de la factura para mostrar en el progreso
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
            var invoiceNumber = invoice?.InvoiceNumber ?? invoiceId.ToString();
            var invoiceSupplier = invoice?.SupplierName ?? "Desconocido";

            // Notificar progreso
            if (_progressNotifier != null)
            {
                await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
                {
                    ProcessedCount = processedCount,
                    TotalCount = totalInvoices,
                    Percentage = percentage,
                    Status = "generating",
                    Message = $"Regenerando asiento {processedCount} de {totalInvoices}...",
                    CurrentInvoiceNumber = invoiceNumber,
                    CurrentInvoiceSupplier = invoiceSupplier,
                    SuccessCount = successCount,
                    ErrorCount = errorCount
                }, cancellationToken);
            }

            try
            {
                var entry = await RegenerateForInvoiceAsync(invoiceId, cancellationToken);
                if (entry != null)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                    var errorMsg = $"No se pudo regenerar el asiento para la factura {invoiceNumber}";
                    errors.Add(errorMsg);
                    
                    if (_progressNotifier != null)
                    {
                        await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
                        {
                            ProcessedCount = processedCount,
                            TotalCount = totalInvoices,
                            Percentage = percentage,
                            Status = "generating",
                            Message = errorMsg,
                            CurrentInvoiceNumber = invoiceNumber,
                            CurrentInvoiceSupplier = invoiceSupplier,
                            SuccessCount = successCount,
                            ErrorCount = errorCount,
                            CurrentError = errorMsg
                        }, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                var errorMsg = $"Error regenerando asiento para factura {invoiceNumber}: {ex.Message}";
                errors.Add(errorMsg);

                // Notificar error
                if (_progressNotifier != null)
                {
                    await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
                    {
                        ProcessedCount = processedCount,
                        TotalCount = totalInvoices,
                        Percentage = percentage,
                        Status = "generating",
                        Message = errorMsg,
                        CurrentInvoiceNumber = invoiceNumber,
                        CurrentInvoiceSupplier = invoiceSupplier,
                        SuccessCount = successCount,
                        ErrorCount = errorCount,
                        CurrentError = ex.Message
                    }, cancellationToken);
                }
            }
        }

        // Verificar si se canceló antes de notificar finalización
        if (cancellationToken.IsCancellationRequested)
        {
            var finalPercentage = totalInvoices > 0 ? (int)((processedCount * 100.0) / totalInvoices) : 0;
            
            // Notificar cancelación
            if (_progressNotifier != null)
            {
                await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
                {
                    ProcessedCount = processedCount,
                    TotalCount = totalInvoices,
                    Percentage = finalPercentage,
                    Status = "cancelled",
                    Message = $"Regeneración cancelada: {successCount} asientos regenerados antes de la cancelación, {errorCount} errores",
                    SuccessCount = successCount,
                    ErrorCount = errorCount
                }, cancellationToken);
            }

            return new GenerateAccountingEntriesResult(
                TotalProcessed: processedCount,
                SuccessCount: successCount,
                ErrorCount: errorCount,
                Errors: errors,
                FailedInvoices: new List<FailedInvoiceInfo>());
        }

        // Notificar finalización
        if (_progressNotifier != null)
        {
            await _progressNotifier.NotifyAsync(new AccountingProgressUpdate
            {
                ProcessedCount = totalInvoices,
                TotalCount = totalInvoices,
                Percentage = 100,
                Status = "completed",
                Message = $"Regeneración completada: {successCount} asientos regenerados, {errorCount} errores",
                SuccessCount = successCount,
                ErrorCount = errorCount
            }, cancellationToken);
        }

        return new GenerateAccountingEntriesResult(
            TotalProcessed: totalInvoices,
            SuccessCount: successCount,
            ErrorCount: errorCount,
            Errors: errors,
            FailedInvoices: new List<FailedInvoiceInfo>());
    }

    private async Task<(AccountingEntry? Entry, string? FailureReason)> GenerateEntryForInvoiceAsync(
        InvoiceReceived invoice,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("Generando asiento para factura {InvoiceNumber} (ID: {InvoiceId}), Total: {TotalAmount}", 
                invoice.InvoiceNumber, invoice.Id, invoice.TotalAmount?.Value);

            // Validar que los campos requeridos no sean null
            if (invoice.TotalAmount == null)
            {
                return (null, "TotalAmount es null");
            }

            if (invoice.SubtotalAmount == null)
            {
                return (null, "SubtotalAmount es null");
            }

            // Obtener o crear las cuentas necesarias
            var suppliersAccount = await GetOrCreateAccountAsync(ACCOUNT_SUPPLIERS, "Proveedores", AccountType.Liability, cancellationToken);
            var expensesAccount = await GetOrCreateAccountAsync(ACCOUNT_EXPENSES, "Compras", AccountType.Expense, cancellationToken);
            var vatAccount = await GetOrCreateAccountAsync(ACCOUNT_VAT_PAYABLE, "H.P. IVA soportado", AccountType.Asset, cancellationToken);
            var bankAccount = await GetOrCreateAccountAsync(ACCOUNT_BANK, "Bancos", AccountType.Asset, cancellationToken);
            var cashAccount = await GetOrCreateAccountAsync(ACCOUNT_CASH, "Caja", AccountType.Asset, cancellationToken);

        // Determinar si es una factura negativa (nota de crédito/descuento)
        bool isCreditNote = invoice.TotalAmount.Value < 0;
        
        // Para facturas negativas, invertir debe/haber
        // Factura normal: DEBE gastos/IVA, HABER proveedores
        // Nota de crédito: HABER gastos/IVA, DEBE proveedores
        var expensesSide = isCreditNote ? EntrySide.Credit : EntrySide.Debit;
        var vatSide = isCreditNote ? EntrySide.Credit : EntrySide.Debit;
        var suppliersSide = isCreditNote ? EntrySide.Debit : EntrySide.Credit;
        var irpfSide = isCreditNote ? EntrySide.Debit : EntrySide.Credit;

        // Crear el asiento
        var entryDescription = isCreditNote 
            ? $"Nota de crédito {invoice.InvoiceNumber} - {invoice.SupplierName}"
            : $"Factura {invoice.InvoiceNumber} - {invoice.SupplierName}";
            
        var entry = new AccountingEntry(
            entryDate: invoice.ReceivedDate,
            description: entryDescription,
            source: EntrySource.Automatic,
            reference: invoice.InvoiceNumber,
            invoiceId: invoice.Id);

        var lines = new List<AccountingEntryLine>();

        // Línea 1: Gastos (Base imponible) - usar valor absoluto y lado según tipo
        lines.Add(new AccountingEntryLine(
            accountingEntryId: entry.Id,
            accountingAccountId: expensesAccount.Id,
            side: expensesSide,
            amount: new Money(Math.Abs(invoice.SubtotalAmount.Value), invoice.SubtotalAmount.Currency),
            description: isCreditNote 
                ? $"Base imponible nota de crédito {invoice.InvoiceNumber}"
                : $"Base imponible factura {invoice.InvoiceNumber}"));

        // Línea 2: IVA (si existe y no es cero)
        if (invoice.TaxAmount != null && invoice.TaxAmount.Value != 0)
        {
            lines.Add(new AccountingEntryLine(
                accountingEntryId: entry.Id,
                accountingAccountId: vatAccount.Id,
                side: vatSide,
                amount: new Money(Math.Abs(invoice.TaxAmount.Value), invoice.TaxAmount.Currency),
                description: isCreditNote
                    ? $"IVA {invoice.TaxRate}% nota de crédito {invoice.InvoiceNumber}"
                    : $"IVA {invoice.TaxRate}% factura {invoice.InvoiceNumber}"));
        }

        // Si hay IRPF, agregar línea adicional
        if (invoice.IrpfAmount != null && invoice.IrpfAmount.Value != 0)
        {
            var irpfAccount = await GetOrCreateAccountAsync(ACCOUNT_IRPF_PAYABLE, "H.P. acreedora por retenciones", AccountType.Liability, cancellationToken);
            
            // Para facturas negativas con IRPF, el IRPF también se invierte
            lines.Add(new AccountingEntryLine(
                accountingEntryId: entry.Id,
                accountingAccountId: irpfAccount.Id,
                side: irpfSide,
                amount: new Money(Math.Abs(invoice.IrpfAmount.Value), invoice.IrpfAmount.Currency),
                description: isCreditNote
                    ? $"IRPF {invoice.IrpfRate}% nota de crédito {invoice.InvoiceNumber}"
                    : $"IRPF {invoice.IrpfRate}% factura {invoice.InvoiceNumber}"));
        }

        // Línea final: Proveedores (Total factura) - usar valor absoluto y lado según tipo
        // NOTA: El total de proveedores es el total de la factura completo, sin restar el IRPF,
        // porque el IRPF se registra como una línea separada en el HABER (retención).
        // El balance es: DEBE (Gastos + IVA) = HABER (Proveedores + IRPF)
        var suppliersAmount = invoice.TotalAmount;
        
        lines.Add(new AccountingEntryLine(
            accountingEntryId: entry.Id,
            accountingAccountId: suppliersAccount.Id,
            side: suppliersSide,
            amount: new Money(Math.Abs(suppliersAmount.Value), suppliersAmount.Currency),
            description: isCreditNote
                ? $"Nota de crédito {invoice.InvoiceNumber} - {invoice.SupplierName}"
                : $"Factura {invoice.InvoiceNumber} - {invoice.SupplierName}"));

        // Agregar todas las líneas al asiento
        entry.ReplaceLines(lines);

        // NOTA: Ya no validamos el balance aquí. Los asientos desbalanceados se generan
        // para que el usuario pueda revisarlos y corregirlos manualmente.

        // Asignar número de asiento al crear (no solo al contabilizar)
        var nextNumber = await _entryRepository.GetNextEntryNumberAsync(entry.EntryYear, cancellationToken);
        entry.AssignEntryNumber(nextNumber);

        _logger?.LogDebug("Asiento generado exitosamente para factura {InvoiceNumber}. EntryNumber: {EntryNumber}, EntryYear: {EntryYear}, Total Debe: {TotalDebit}, Total Haber: {TotalCredit}", 
            invoice.InvoiceNumber, entry.EntryNumber, entry.EntryYear, entry.GetTotalDebit().Value, entry.GetTotalCredit().Value);

        return (entry, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generando asiento para factura {InvoiceNumber} (ID: {InvoiceId})", invoice.InvoiceNumber, invoice.Id);
            return (null, ex.Message);
        }
    }

    private async Task<AccountingAccount> GetOrCreateAccountAsync(
        string code,
        string name,
        AccountType type,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByCodeAsync(code, cancellationToken);
        if (account != null)
        {
            return account;
        }

        // Crear la cuenta si no existe
        account = new AccountingAccount(code, name, type);
        await _accountRepository.AddAsync(account, cancellationToken);
        
        return account;
    }
}

