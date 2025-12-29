using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Banking;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Consultas agregadas para el dashboard.
/// Mueve la lógica de agregación fuera de los controladores.
/// </summary>
public class DashboardUseCase : IDashboardUseCase
{
    private readonly IInvoiceReceivedRepository _invoiceRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;
    private readonly ITaskRepository _taskRepository;

    public DashboardUseCase(
        IInvoiceReceivedRepository invoiceRepository,
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository,
        ITaskRepository taskRepository)
    {
        _invoiceRepository = invoiceRepository;
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _taskRepository = taskRepository;
    }

    public async Task<DashboardTasksResult> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new List<DashboardTaskDto>();

        var storedTasks = await _taskRepository.GetPendingTasksAsync(null, cancellationToken);
        foreach (var task in storedTasks)
        {
            tasks.Add(new DashboardTaskDto(
                task.Id,
                task.Title,
                task.Description,
                task.RelatedInvoiceId.HasValue ? "invoice" :
                    task.RelatedTransactionId.HasValue ? "bank" : "system",
                task.Priority == TaskPriority.High ? "high" :
                    task.Priority == TaskPriority.Medium ? "medium" : "low",
                task.IsCompleted ? "completed" : "pending",
                task.IsPinned,
                task.DueDate,
                task.CreatedAt,
                task.CreatedAt,
                task.RelatedInvoiceId.HasValue
                    ? new Dictionary<string, object> { { "invoiceId", task.RelatedInvoiceId.Value } }
                    : null));
        }

        var allInvoices = await _invoiceRepository.GetAllAsync(cancellationToken);
        var invoicesWithTasks = storedTasks
            .Where(t => t.RelatedInvoiceId.HasValue)
            .Select(t => t.RelatedInvoiceId!.Value)
            .ToHashSet();

        foreach (var invoice in allInvoices)
        {
            if (invoicesWithTasks.Contains(invoice.Id))
                continue;

            if (invoice.SupplierName == "<desconocido>" ||
                (invoice.ExtractionConfidence.HasValue && invoice.ExtractionConfidence.Value < 0.7m))
            {
                var priority = invoice.ExtractionConfidence.HasValue && invoice.ExtractionConfidence.Value < 0.5m
                    ? "high" : "medium";

                tasks.Add(new DashboardTaskDto(
                    Guid.NewGuid(),
                    $"Verificar factura {invoice.InvoiceNumber}",
                    $"Factura de {invoice.SupplierName} requiere verificación manual. Confianza: {(invoice.ExtractionConfidence * 100):F0}%",
                    "invoice",
                    priority,
                    "pending",
                    false,
                    invoice.CreatedAt, // sin due date, usamos created como referencia
                    invoice.CreatedAt,
                    invoice.UpdatedAt,
                    new Dictionary<string, object>
                    {
                        { "invoiceId", invoice.Id },
                        { "invoiceNumber", invoice.InvoiceNumber },
                        { "supplierName", invoice.SupplierName },
                        { "extractionConfidence", invoice.ExtractionConfidence ?? 0 },
                        { "isDynamic", true }
                    }));
            }
        }

        var orderedTasks = tasks
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Priority == "high" ? 3 : t.Priority == "medium" ? 2 : 1)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ToList();

        return new DashboardTasksResult(orderedTasks);
    }

    public async Task<BankSummaryDto> GetBankBalancesAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _bankAccountRepository.GetAllActiveAsync(cancellationToken);
        var banks = new List<BankBalanceDto>();
        decimal totalBalance = 0;
        const string primaryCurrency = "EUR";

        foreach (var account in accounts)
        {
            var transactions = await _bankTransactionRepository.GetByAccountIdAsync(
                account.Id,
                cancellationToken);

            var balance = transactions.Sum(t => t.Amount.Value);
            var lastTransaction = transactions
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            banks.Add(new BankBalanceDto(
                account.Id,
                account.BankName,
                account.AccountNumber,
                balance,
                primaryCurrency,
                lastTransaction?.TransactionDate,
                lastTransaction?.Amount.Value));

            totalBalance += balance;
        }

        return new BankSummaryDto(
            banks,
            totalBalance,
            primaryCurrency,
            null,
            null,
            null,
            null);
    }

    public async Task<IEnumerable<AlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = new List<AlertDto>();

        var allInvoices = await _invoiceRepository.GetAllAsync(cancellationToken);
        var lowConfidenceInvoices = allInvoices
            .Where(i => i.ExtractionConfidence.HasValue && i.ExtractionConfidence.Value < 0.7m)
            .ToList();

        foreach (var invoice in lowConfidenceInvoices)
        {
            var confidencePercent = invoice.ExtractionConfidence!.Value * 100;
            alerts.Add(new AlertDto(
                Guid.NewGuid(),
                "system",
                confidencePercent < 50 ? "critical" : confidencePercent < 70 ? "high" : "medium",
                $"Baja confianza en factura: {invoice.InvoiceNumber}",
                $"La factura de {invoice.SupplierName} tiene una confianza de extracción del {confidencePercent:F0}%. Se recomienda verificación manual. Importe: {invoice.TotalAmount.Value:F2} {invoice.TotalAmount.Currency}",
                invoice.CreatedAt,
                null,
                null,
                new Dictionary<string, object>
                {
                    { "invoiceId", invoice.Id },
                    { "invoiceNumber", invoice.InvoiceNumber },
                    { "extractionConfidence", invoice.ExtractionConfidence.Value }
                }));
        }

        var accounts = await _bankAccountRepository.GetAllActiveAsync(cancellationToken);
        foreach (var account in accounts)
        {
            var transactions = await _bankTransactionRepository.GetByAccountIdAsync(
                account.Id,
                cancellationToken);

            var balance = transactions.Sum(t => t.Amount.Value);

            if (balance < 1000)
            {
                alerts.Add(new AlertDto(
                    Guid.NewGuid(),
                    "low_balance",
                    balance < 100 ? "critical" : balance < 500 ? "high" : "medium",
                    $"Saldo bajo en {account.BankName}",
                    $"El saldo de la cuenta {account.AccountNumber} es {balance:F2} EUR, por debajo del umbral recomendado.",
                    DateTime.UtcNow,
                    null,
                    null,
                    new Dictionary<string, object>
                    {
                        { "bankAccountId", account.Id },
                        { "balance", balance }
                    }));
            }
        }

        var orderedAlerts = alerts
            .OrderByDescending(a => a.Severity == "critical" ? 5 :
                                    a.Severity == "high" ? 4 :
                                    a.Severity == "medium" ? 3 :
                                    a.Severity == "low" ? 2 : 1)
            .ThenByDescending(a => a.OccurredAt)
            .ToList();

        return orderedAlerts;
    }
}

