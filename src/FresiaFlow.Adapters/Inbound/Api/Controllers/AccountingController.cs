using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Accounting;
using FresiaFlow.Adapters.Inbound.Api.Notifiers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using FresiaFlow.Application.Services;
using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador API para gestión de contabilidad.
/// </summary>
[ApiController]
[Route("api/accounting")]
public class AccountingController : ControllerBase
{
    private readonly IGenerateAccountingEntriesUseCase _generateEntriesUseCase;
    private readonly IGetAccountingEntriesUseCase _getEntriesUseCase;
    private readonly IUpdateAccountingEntryUseCase _updateEntryUseCase;
    private readonly IPostAccountingEntryUseCase _postEntryUseCase;
    private readonly IAccountingEntryRepository _entryRepository;
    private readonly IAccountingAccountRepository _accountRepository;
    private readonly IInvoiceReceivedRepository _invoiceRepository;
    private readonly AccountingGenerationCancellationService _cancellationService;
    private readonly ILogger<AccountingController> _logger;

    public AccountingController(
        IGenerateAccountingEntriesUseCase generateEntriesUseCase,
        IGetAccountingEntriesUseCase getEntriesUseCase,
        IUpdateAccountingEntryUseCase updateEntryUseCase,
        IPostAccountingEntryUseCase postEntryUseCase,
        IAccountingEntryRepository entryRepository,
        IAccountingAccountRepository accountRepository,
        IInvoiceReceivedRepository invoiceRepository,
        AccountingGenerationCancellationService cancellationService,
        ILogger<AccountingController> logger)
    {
        _generateEntriesUseCase = generateEntriesUseCase;
        _getEntriesUseCase = getEntriesUseCase;
        _updateEntryUseCase = updateEntryUseCase;
        _postEntryUseCase = postEntryUseCase;
        _entryRepository = entryRepository;
        _accountRepository = accountRepository;
        _invoiceRepository = invoiceRepository;
        _cancellationService = cancellationService;
        _logger = logger;
    }

    /// <summary>
    /// Genera asientos contables automáticamente para todas las facturas que aún no tienen asiento.
    /// </summary>
    [HttpPost("entries/generate")]
    public async Task<IActionResult> GenerateEntries(CancellationToken cancellationToken)
    {
        // #region agent log
        try {
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                location = "AccountingController.cs:52", 
                message = "GenerateEntries endpoint called", 
                data = new { 
                    cancellationTokenIsCancellationRequested = cancellationToken.IsCancellationRequested
                }, 
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                sessionId = "debug-session", 
                runId = "run1", 
                hypothesisId = "F" 
            }) + "\n";
            System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
        } catch (Exception) { /* Ignore log errors */ }
        // #endregion
        
        try
        {
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "AccountingController.cs:56", 
                    message = "Calling ExecuteAsync", 
                    data = new { }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "F" 
                }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
            
            var result = await _generateEntriesUseCase.ExecuteAsync(cancellationToken);
            
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "AccountingController.cs:58", 
                    message = "ExecuteAsync completed", 
                    data = new { 
                        totalProcessed = result.TotalProcessed,
                        successCount = result.SuccessCount,
                        errorCount = result.ErrorCount,
                        errorsCount = result.Errors?.Count ?? 0
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "F" 
                }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "AccountingController.cs:60", 
                    message = "Exception caught in GenerateEntries", 
                    data = new { 
                        exceptionType = ex.GetType().Name,
                        exceptionMessage = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "F" 
                }) + "\n";
                System.IO.File.AppendAllText(@"c:\repo\FresiaFlow\.cursor\debug.log", logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
            
            _logger.LogError(ex, "Error generando asientos contables");
            return StatusCode(500, new { error = "Error generando asientos contables", message = ex.Message });
        }
    }

    /// <summary>
    /// Regenera todos los asientos contables automáticamente (elimina los existentes y crea nuevos).
    /// IMPORTANTE: Esta ruta debe ir antes de las rutas con parámetros para evitar conflictos de routing.
    /// </summary>
    [HttpPost("entries/regenerate")]
    public async Task<IActionResult> RegenerateAllEntries(CancellationToken cancellationToken)
    {
        try
        {
            // Crear token de cancelación específico para esta operación
            var operationToken = _cancellationService.CreateCancellationToken();
            
            // Combinar con el token de la petición HTTP
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, operationToken);

            // Obtener todos los asientos automáticos en estado Draft
            var allEntries = await _entryRepository.GetFilteredAsync(
                status: EntryStatus.Draft,
                source: EntrySource.Automatic,
                cancellationToken: combinedCts.Token);

            // Extraer los invoiceIds de los asientos
            var invoiceIds = allEntries
                .Where(e => e.InvoiceId.HasValue)
                .Select(e => e.InvoiceId!.Value)
                .Distinct()
                .ToList();

            if (invoiceIds.Count == 0)
            {
                _cancellationService.Clear();
                return Ok(new GenerateAccountingEntriesResult(
                    TotalProcessed: 0,
                    SuccessCount: 0,
                    ErrorCount: 0,
                    Errors: new List<string>(),
                    FailedInvoices: new List<FailedInvoiceInfo>()));
            }

            var result = await _generateEntriesUseCase.RegenerateForInvoicesAsync(invoiceIds, combinedCts.Token);
            _cancellationService.Clear();
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _cancellationService.Clear();
            _logger.LogInformation("Regeneración de asientos cancelada por el usuario");
            return Ok(new GenerateAccountingEntriesResult(
                TotalProcessed: 0,
                SuccessCount: 0,
                ErrorCount: 0,
                Errors: new List<string> { "Operación cancelada por el usuario" },
                FailedInvoices: new List<FailedInvoiceInfo>()));
        }
        catch (Exception ex)
        {
            _cancellationService.Clear();
            _logger.LogError(ex, "Error regenerando todos los asientos contables");
            return StatusCode(500, new { error = "Error regenerando asientos contables", message = ex.Message });
        }
    }

    /// <summary>
    /// Genera un asiento contable para una factura específica.
    /// </summary>
    [HttpPost("entries/generate/{invoiceId:guid}")]
    public async Task<IActionResult> GenerateEntryForInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _generateEntriesUseCase.GenerateForInvoiceAsync(invoiceId, cancellationToken);
            if (entry == null)
            {
                return BadRequest(new { error = "No se pudo generar el asiento. Puede que ya exista uno para esta factura." });
            }

            return Ok(MapToDto(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando asiento para factura {InvoiceId}", invoiceId);
            return StatusCode(500, new { error = "Error generando asiento contable", message = ex.Message });
        }
    }

    /// <summary>
    /// Regenera asientos contables para las facturas especificadas.
    /// IMPORTANTE: Esta ruta debe ir antes de las rutas con parámetros para evitar conflictos de routing.
    /// </summary>
    [HttpPost("entries/regenerate/selected")]
    public async Task<IActionResult> RegenerateSelectedEntries(
        [FromBody] RegenerateEntriesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.EntryIds == null || request.EntryIds.Count == 0)
            {
                return BadRequest(new { error = "Debe especificar al menos un asiento para regenerar" });
            }

            // Crear token de cancelación específico para esta operación
            var operationToken = _cancellationService.CreateCancellationToken();
            
            // Combinar con el token de la petición HTTP
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, operationToken);

            // Obtener los asientos seleccionados
            var entries = new List<AccountingEntry>();
            foreach (var entryId in request.EntryIds)
            {
                var entry = await _entryRepository.GetByIdAsync(entryId, combinedCts.Token);
                if (entry != null && entry.InvoiceId.HasValue)
                {
                    entries.Add(entry);
                }
            }

            // Extraer los invoiceIds
            var invoiceIds = entries
                .Where(e => e.InvoiceId.HasValue)
                .Select(e => e.InvoiceId!.Value)
                .Distinct()
                .ToList();

            if (invoiceIds.Count == 0)
            {
                _cancellationService.Clear();
                return BadRequest(new { error = "No se encontraron facturas asociadas a los asientos seleccionados" });
            }

            var result = await _generateEntriesUseCase.RegenerateForInvoicesAsync(invoiceIds, combinedCts.Token);
            _cancellationService.Clear();
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _cancellationService.Clear();
            _logger.LogInformation("Regeneración de asientos seleccionados cancelada por el usuario");
            return Ok(new GenerateAccountingEntriesResult(
                TotalProcessed: 0,
                SuccessCount: 0,
                ErrorCount: 0,
                Errors: new List<string> { "Operación cancelada por el usuario" },
                FailedInvoices: new List<FailedInvoiceInfo>()));
        }
        catch (Exception ex)
        {
            _cancellationService.Clear();
            _logger.LogError(ex, "Error regenerando asientos seleccionados");
            return StatusCode(500, new { error = "Error regenerando asientos contables", message = ex.Message });
        }
    }

    /// <summary>
    /// Cancela la operación de generación/regeneración de asientos en progreso.
    /// </summary>
    [HttpPost("entries/generation/cancel")]
    public IActionResult CancelGeneration()
    {
        try
        {
            if (!_cancellationService.IsOperationInProgress())
            {
                return BadRequest(new { error = "No hay ninguna operación de generación en progreso" });
            }

            _cancellationService.Cancel();
            _logger.LogInformation("Cancelación de generación de asientos solicitada por el usuario");
            return Ok(new { message = "Operación de generación cancelada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelando generación de asientos");
            return StatusCode(500, new { error = "Error cancelando generación", message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene asientos contables con filtros opcionales.
    /// </summary>
    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] EntryStatus? status = null,
        [FromQuery] EntrySource? source = null,
        [FromQuery] Guid? invoiceId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _getEntriesUseCase.ExecuteAsync(
                startDate: startDate,
                endDate: endDate,
                status: status,
                source: source,
                invoiceId: invoiceId,
                cancellationToken: cancellationToken);

            return Ok(entries.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo asientos contables");
            return StatusCode(500, new { error = "Error obteniendo asientos contables", message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene un asiento contable por ID.
    /// </summary>
    [HttpGet("entries/{id:guid}")]
    public async Task<IActionResult> GetEntry(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _entryRepository.GetByIdAsync(id, cancellationToken);
            
            if (entry == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo asiento {EntryId}", id);
            return StatusCode(500, new { error = "Error obteniendo asiento contable", message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene una factura por ID (para verla desde contabilidad).
    /// </summary>
    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
            
            if (invoice == null)
            {
                return NotFound();
            }

            // Verificar si tiene asiento
            var hasEntry = await _entryRepository.ExistsForInvoiceAsync(id, cancellationToken);
            
            // Mapear a DTO usando el mismo formato que InvoicesController
            var dto = new
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                ReceivedDate = invoice.ReceivedDate,
                SupplierName = invoice.SupplierName,
                SupplierTaxId = invoice.SupplierTaxId,
                SupplierAddress = invoice.SupplierAddress,
                SubtotalAmount = invoice.SubtotalAmount?.Value,
                TaxAmount = invoice.TaxAmount?.Value,
                TaxRate = invoice.TaxRate,
                IrpfAmount = invoice.IrpfAmount?.Value,
                IrpfRate = invoice.IrpfRate,
                TotalAmount = invoice.TotalAmount?.Value,
                Currency = invoice.Currency,
                PaymentType = invoice.PaymentType.ToString(),
                Origin = invoice.Origin.ToString(),
                OriginalFilePath = invoice.OriginalFilePath,
                ProcessedFilePath = invoice.ProcessedFilePath,
                HasAccountingEntry = hasEntry,
                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo factura {InvoiceId}", id);
            return StatusCode(500, new { error = "Error obteniendo factura", message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene las facturas que no pudieron generar asientos.
    /// </summary>
    [HttpGet("failed-invoices")]
    public async Task<IActionResult> GetFailedInvoices(CancellationToken cancellationToken)
    {
        try
        {
            // Obtener todas las facturas
            var allInvoices = await _invoiceRepository.GetAllAsync(cancellationToken);
            var allInvoiceIds = allInvoices.Select(i => i.Id).ToList();
            
            // Obtener facturas que tienen asientos
            var invoicesWithEntries = new HashSet<Guid>();
            foreach (var invoiceId in allInvoiceIds)
            {
                var exists = await _entryRepository.ExistsForInvoiceAsync(invoiceId, cancellationToken);
                if (exists)
                {
                    invoicesWithEntries.Add(invoiceId);
                }
            }
            
            // Filtrar facturas sin asientos
            var failedInvoices = allInvoices
                .Where(i => !invoicesWithEntries.Contains(i.Id))
                .Select(i => new FailedInvoiceDto
                {
                    InvoiceId = i.Id,
                    InvoiceNumber = i.InvoiceNumber ?? "Sin número",
                    SupplierName = i.SupplierName ?? "Sin proveedor",
                    Reason = i.TotalAmount == null ? "TotalAmount es null" 
                        : i.SubtotalAmount == null ? "SubtotalAmount es null"
                        : "Razón desconocida"
                })
                .ToList();

            return Ok(failedInvoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo facturas fallidas");
            return StatusCode(500, new { error = "Error obteniendo facturas fallidas", message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un asiento contable.
    /// </summary>
    [HttpPut("entries/{id:guid}")]
    public async Task<IActionResult> UpdateEntry(
        Guid id,
        [FromBody] UpdateAccountingEntryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Mapear DTOs del controlador a DTOs del puerto
            var portLines = request.Lines?.Select(l =>
            {
                // El Side puede venir como string desde el frontend, convertirlo a enum
                EntrySide side;
                try
                {
                    // Intentar convertir directamente si ya es enum
                    side = l.Side;
                }
                catch
                {
                    // Si falla, intentar parsear desde string o int
                    var sideValue = l.Side.ToString();
                    if (Enum.TryParse<EntrySide>(sideValue, true, out var parsedSide))
                    {
                        side = parsedSide;
                    }
                    else if (int.TryParse(sideValue, out var sideInt))
                    {
                        side = (EntrySide)sideInt;
                    }
                    else
                    {
                        // Default basado en el string
                        side = sideValue.Equals("Debit", StringComparison.OrdinalIgnoreCase) 
                            ? EntrySide.Debit 
                            : EntrySide.Credit;
                    }
                }

                return new Application.Ports.Inbound.AccountingEntryLineDto(
                    l.AccountingAccountId,
                    side,
                    l.Amount,
                    l.Currency ?? "EUR",
                    l.Description,
                    l.Id); // Incluir ID si está presente (último parámetro opcional)
            });

            var entry = await _updateEntryUseCase.ExecuteAsync(
                entryId: id,
                description: request.Description,
                entryDate: request.EntryDate,
                lines: portLines,
                notes: request.Notes,
                cancellationToken: cancellationToken);

            return Ok(MapToDto(entry));
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando asiento {EntryId}", id);
            return StatusCode(500, new { error = "Error actualizando asiento contable", message = ex.Message });
        }
    }

    /// <summary>
    /// Contabiliza (post) un asiento.
    /// </summary>
    [HttpPost("entries/{id:guid}/post")]
    public async Task<IActionResult> PostEntry(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _postEntryUseCase.ExecuteAsync(id, cancellationToken);
            return Ok(new { message = "Asiento contabilizado correctamente" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contabilizando asiento {EntryId}", id);
            return StatusCode(500, new { error = "Error contabilizando asiento", message = ex.Message });
        }
    }

    /// <summary>
    /// Contabiliza todos los asientos balanceados en estado Draft.
    /// </summary>
    [HttpPost("entries/post-balanced")]
    public async Task<IActionResult> PostAllBalancedEntries(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _postEntryUseCase.PostAllBalancedEntriesAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contabilizando asientos balanceados");
            return StatusCode(500, new { error = "Error contabilizando asientos balanceados", message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene todas las cuentas contables.
    /// </summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _accountRepository.GetAllAsync(cancellationToken);
            return Ok(accounts.Select(a => new AccountingAccountDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Type = a.Type,
                IsActive = a.IsActive
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo cuentas contables");
            return StatusCode(500, new { error = "Error obteniendo cuentas contables", message = ex.Message });
        }
    }

    /// <summary>
    /// Crea o actualiza una cuenta contable.
    /// </summary>
    [HttpPost("accounts")]
    public async Task<IActionResult> CreateOrUpdateAccount(
        [FromBody] CreateOrUpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AccountingAccount account;
            
            if (request.Id.HasValue)
            {
                // Actualizar cuenta existente
                account = await _accountRepository.GetByIdAsync(request.Id.Value, cancellationToken);
                if (account == null)
                {
                    return NotFound(new { error = "Cuenta no encontrada" });
                }
                
                account.UpdateName(request.Name);
                if (request.IsActive.HasValue)
                {
                    if (request.IsActive.Value)
                        account.Activate();
                    else
                        account.Deactivate();
                }
                
                await _accountRepository.UpdateAsync(account, cancellationToken);
            }
            else
            {
                // Crear nueva cuenta
                account = new AccountingAccount(request.Code, request.Name, request.Type);
                await _accountRepository.AddAsync(account, cancellationToken);
            }

            return Ok(new AccountingAccountDto
            {
                Id = account.Id,
                Code = account.Code,
                Name = account.Name,
                Type = account.Type,
                IsActive = account.IsActive
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando cuenta contable");
            return StatusCode(500, new { error = "Error guardando cuenta contable", message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una cuenta contable (desactiva).
    /// </summary>
    [HttpDelete("accounts/{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
            if (account == null)
            {
                return NotFound(new { error = "Cuenta no encontrada" });
            }

            account.Deactivate();
            await _accountRepository.UpdateAsync(account, cancellationToken);
            
            return Ok(new { message = "Cuenta desactivada correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando cuenta {AccountId}", id);
            return StatusCode(500, new { error = "Error eliminando cuenta contable", message = ex.Message });
        }
    }

    private AccountingEntryDto MapToDto(AccountingEntry entry)
    {
        return new AccountingEntryDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryYear = entry.EntryYear,
            EntryDate = entry.EntryDate,
            Description = entry.Description,
            Reference = entry.Reference,
            InvoiceId = entry.InvoiceId,
            Source = entry.Source,
            Status = entry.Status,
            IsReversed = entry.IsReversed,
            ReversedByEntryId = entry.ReversedByEntryId,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            Lines = entry.Lines.Select(l => new AccountingEntryLineDto
            {
                Id = l.Id,
                AccountingAccountId = l.AccountingAccountId,
                Side = l.Side,
                Amount = l.Amount.Value,
                Currency = l.Amount.Currency,
                Description = l.Description
            }).ToList(),
            TotalDebit = entry.GetTotalDebit().Value,
            TotalCredit = entry.GetTotalCredit().Value,
            IsBalanced = entry.IsBalanced()
        };
    }
}

/// <summary>
/// DTO para actualizar un asiento contable.
/// </summary>
public class UpdateAccountingEntryRequest
{
    public string? Description { get; set; }
    public DateTime? EntryDate { get; set; }
    public List<AccountingEntryLineRequestDto>? Lines { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para regenerar asientos seleccionados.
/// </summary>
public class RegenerateEntriesRequest
{
    public List<Guid> EntryIds { get; set; } = new();
}

/// <summary>
/// DTO para representar un asiento contable.
/// </summary>
public class AccountingEntryDto
{
    public Guid Id { get; set; }
    public int? EntryNumber { get; set; }
    public int EntryYear { get; set; }
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public Guid? InvoiceId { get; set; }
    public EntrySource Source { get; set; }
    public EntryStatus Status { get; set; }
    public bool IsReversed { get; set; }
    public Guid? ReversedByEntryId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AccountingEntryLineDto> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
}

/// <summary>
/// DTO para representar una línea de asiento contable (respuesta).
/// </summary>
public class AccountingEntryLineDto
{
    public Guid Id { get; set; }
    public Guid AccountingAccountId { get; set; }
    public EntrySide Side { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? Description { get; set; }
}

/// <summary>
/// DTO para crear/actualizar una línea de asiento contable (request).
/// </summary>
public class AccountingEntryLineRequestDto
{
    public Guid? Id { get; set; } // ID opcional para preservar líneas existentes
    public Guid AccountingAccountId { get; set; }
    public EntrySide Side { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? Description { get; set; }
}

/// <summary>
/// DTO para representar una cuenta contable.
/// </summary>
public class AccountingAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO para crear o actualizar una cuenta contable.
/// </summary>
public class CreateOrUpdateAccountRequest
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO para representar una factura que no pudo generar asiento.
/// </summary>
public class FailedInvoiceDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

