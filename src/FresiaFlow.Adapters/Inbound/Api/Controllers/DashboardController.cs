using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Banking;
using Microsoft.AspNetCore.Mvc;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para el Dashboard.
/// Proporciona datos agregados para la vista principal del sistema.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IInvoiceReceivedRepository _invoiceReceivedRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;

    public DashboardController(
        IInvoiceReceivedRepository invoiceReceivedRepository,
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository)
    {
        _invoiceReceivedRepository = invoiceReceivedRepository;
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
    }

    /// <summary>
    /// Obtiene todas las tareas pendientes del dashboard.
    /// </summary>
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(CancellationToken cancellationToken)
    {
        var tasks = new List<DashboardTaskDto>();

        // En el modelo contable, todas las facturas están contabilizadas.
        // No hay tareas de "revisión" porque no hay estados ficticios.
        // Las tareas de facturas se generan solo si hay problemas reales (ej: facturas sin proveedor identificado)
        var allInvoices = await _invoiceReceivedRepository.GetAllAsync(cancellationToken);
        
        // Tareas para facturas con proveedor desconocido o confianza baja
        foreach (var invoice in allInvoices)
        {
            if (invoice.SupplierName == "<desconocido>" || 
                (invoice.ExtractionConfidence.HasValue && invoice.ExtractionConfidence.Value < 0.7m))
            {
                tasks.Add(new DashboardTaskDto
                {
                    Id = Guid.NewGuid(),
                    Title = $"Verificar factura {invoice.InvoiceNumber}",
                    Description = $"Factura de {invoice.SupplierName} requiere verificación manual. Confianza: {(invoice.ExtractionConfidence * 100):F0}%",
                    Type = "invoice",
                    Priority = invoice.ExtractionConfidence.HasValue && invoice.ExtractionConfidence.Value < 0.5m ? "high" : "medium",
                    Status = "pending",
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,
                    Metadata = new Dictionary<string, object>
                    {
                        { "invoiceId", invoice.Id },
                        { "invoiceNumber", invoice.InvoiceNumber },
                        { "supplierName", invoice.SupplierName },
                        { "extractionConfidence", invoice.ExtractionConfidence ?? 0 }
                    }
                });
            }
        }

        // Ordenar por prioridad (high primero) y luego por fecha límite
        var orderedTasks = tasks
            .OrderByDescending(t => t.Priority == "high" ? 3 : t.Priority == "medium" ? 2 : 1)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ToList();

        return Ok(orderedTasks);
    }

    /// <summary>
    /// Obtiene el resumen de saldos bancarios.
    /// </summary>
    [HttpGet("bank-balances")]
    public async Task<IActionResult> GetBankBalances(CancellationToken cancellationToken)
    {
        var accounts = await _bankAccountRepository.GetAllActiveAsync(cancellationToken);
        var banks = new List<BankBalanceDto>();
        decimal totalBalance = 0;
        string primaryCurrency = "EUR";

        foreach (var account in accounts)
        {
            // Calcular saldo actual sumando todas las transacciones
            var transactions = await _bankTransactionRepository.GetByAccountIdAsync(
                account.Id, 
                cancellationToken);
            
            // El saldo es la suma de todas las transacciones (positivas y negativas)
            var balance = transactions.Sum(t => t.Amount.Value);
            
            // Obtener última transacción
            var lastTransaction = transactions
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            var bankBalance = new BankBalanceDto
            {
                BankId = account.Id,
                BankName = account.BankName,
                AccountNumber = account.AccountNumber,
                Balance = balance,
                Currency = primaryCurrency, // Simplificado: asumimos EUR
                LastMovementDate = lastTransaction?.TransactionDate,
                LastMovementAmount = lastTransaction?.Amount.Value
            };

            banks.Add(bankBalance);
            totalBalance += balance;
        }

        // Calcular variaciones (simplificado: por ahora no hay historial)
        var summary = new BankSummaryDto
        {
            Banks = banks,
            TotalBalance = totalBalance,
            PrimaryCurrency = primaryCurrency
            // previousDayBalance, previousDayVariation, previousMonthBalance, previousMonthVariation
            // se pueden agregar cuando haya historial de saldos
        };

        return Ok(summary);
    }

    /// <summary>
    /// Obtiene todas las alertas activas.
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken cancellationToken)
    {
        var alerts = new List<AlertDto>();

        // Alertas de facturas vencidas
        // NOTA: En el modelo contable no hay "vencimiento" porque las facturas están contabilizadas.
        // Se pueden generar alertas para facturas con baja confianza de extracción.
        var allInvoices = await _invoiceReceivedRepository.GetAllAsync(cancellationToken);
        var lowConfidenceInvoices = allInvoices
            .Where(i => i.ExtractionConfidence.HasValue && i.ExtractionConfidence.Value < 0.7m)
            .ToList();

        foreach (var invoice in lowConfidenceInvoices)
        {
            var confidencePercent = (invoice.ExtractionConfidence!.Value * 100);
            alerts.Add(new AlertDto
            {
                Id = Guid.NewGuid(),
                Type = "system",
                Severity = confidencePercent < 50 ? "critical" : confidencePercent < 70 ? "high" : "medium",
                Title = $"Baja confianza en factura: {invoice.InvoiceNumber}",
                Description = $"La factura de {invoice.SupplierName} tiene una confianza de extracción del {confidencePercent:F0}%. Se recomienda verificación manual. Importe: {invoice.TotalAmount.Value:F2} {invoice.TotalAmount.Currency}",
                OccurredAt = invoice.CreatedAt,
                Metadata = new Dictionary<string, object>
                {
                    { "invoiceId", invoice.Id },
                    { "invoiceNumber", invoice.InvoiceNumber },
                    { "extractionConfidence", invoice.ExtractionConfidence.Value }
                }
            });
        }

        // Alertas de saldos bajos (simplificado: alerta si saldo < 1000 EUR)
        var accounts = await _bankAccountRepository.GetAllActiveAsync(cancellationToken);
        foreach (var account in accounts)
        {
            var transactions = await _bankTransactionRepository.GetByAccountIdAsync(
                account.Id, 
                cancellationToken);
            
            var balance = transactions.Sum(t => t.Amount.Value);

            if (balance < 1000)
            {
                alerts.Add(new AlertDto
                {
                    Id = Guid.NewGuid(),
                    Type = "low_balance",
                    Severity = balance < 100 ? "critical" : balance < 500 ? "high" : "medium",
                    Title = $"Saldo bajo en {account.BankName}",
                    Description = $"El saldo de la cuenta {account.AccountNumber} es {balance:F2} EUR, por debajo del umbral recomendado.",
                    OccurredAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        { "bankAccountId", account.Id },
                        { "balance", balance }
                    }
                });
            }
        }

        // Ordenar por severidad (critical primero) y luego por fecha (más recientes primero)
        var orderedAlerts = alerts
            .OrderByDescending(a => a.Severity == "critical" ? 5 : 
                                    a.Severity == "high" ? 4 : 
                                    a.Severity == "medium" ? 3 : 
                                    a.Severity == "low" ? 2 : 1)
            .ThenByDescending(a => a.OccurredAt)
            .ToList();

        return Ok(orderedAlerts);
    }
}

// DTOs para las respuestas
public class DashboardTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class BankBalanceDto
{
    public Guid BankId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime? LastMovementDate { get; set; }
    public decimal? LastMovementAmount { get; set; }
}

public class BankSummaryDto
{
    public List<BankBalanceDto> Banks { get; set; } = new();
    public decimal TotalBalance { get; set; }
    public string PrimaryCurrency { get; set; } = "EUR";
    public decimal? PreviousDayBalance { get; set; }
    public decimal? PreviousDayVariation { get; set; }
    public decimal? PreviousMonthBalance { get; set; }
    public decimal? PreviousMonthVariation { get; set; }
}

public class AlertDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

