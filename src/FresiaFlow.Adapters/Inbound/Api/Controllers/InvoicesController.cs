using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Adapters.Inbound.Api.Dtos;
using FresiaFlow.Adapters.Outbound.Excel;
using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para gestión de facturas recibidas.
/// Expone todos los datos fiscales, económicos y de detalle extraídos por OpenAI.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IUploadInvoiceUseCase _uploadInvoiceUseCase;
    private readonly IGetAllInvoicesUseCase _getAllInvoicesUseCase;
    private readonly IDeleteInvoiceUseCase _deleteInvoiceUseCase;
    private readonly IUpdateInvoiceSupplierUseCase _updateInvoiceSupplierUseCase;
    private readonly IUpdateInvoiceUseCase _updateInvoiceUseCase;
    private readonly IInvoiceReceivedRepository _invoiceReceivedRepository;
    private readonly IOpenAIClient _openAIClient;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IUploadInvoiceUseCase uploadInvoiceUseCase,
        IGetAllInvoicesUseCase getAllInvoicesUseCase,
        IDeleteInvoiceUseCase deleteInvoiceUseCase,
        IUpdateInvoiceSupplierUseCase updateInvoiceSupplierUseCase,
        IUpdateInvoiceUseCase updateInvoiceUseCase,
        IInvoiceReceivedRepository invoiceReceivedRepository,
        IOpenAIClient openAIClient,
        ILogger<InvoicesController> logger)
    {
        _uploadInvoiceUseCase = uploadInvoiceUseCase;
        _getAllInvoicesUseCase = getAllInvoicesUseCase;
        _deleteInvoiceUseCase = deleteInvoiceUseCase;
        _updateInvoiceSupplierUseCase = updateInvoiceSupplierUseCase;
        _updateInvoiceUseCase = updateInvoiceUseCase;
        _invoiceReceivedRepository = invoiceReceivedRepository;
        _openAIClient = openAIClient;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las facturas recibidas con todos sus datos fiscales y de detalle.
    /// Soporta filtros contables: año, trimestre, proveedor, tipo de pago.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllInvoices(
        [FromQuery] int? year,
        [FromQuery] int? quarter,
        [FromQuery] string? supplierName,
        [FromQuery] string? paymentType,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GetAllInvoices - Filtros recibidos: Year={Year}, Quarter={Quarter}, SupplierName={SupplierName}, PaymentType={PaymentType}",
                year, quarter, supplierName, paymentType);
            
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "InvoicesController.cs:60", 
                    message = "GetAllInvoices entry", 
                    data = new { 
                        year = year?.ToString() ?? "null", 
                        quarter = quarter?.ToString() ?? "null", 
                        supplierName = supplierName ?? "null", 
                        paymentType = paymentType ?? "null",
                        yearHasValue = year.HasValue,
                        quarterHasValue = quarter.HasValue
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "C" 
                }) + "\n";
                SafeAppendToLogFile(logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion

            // Usar el repositorio directamente para aprovechar los filtros optimizados
            PaymentType? paymentTypeEnum = null;
            if (!string.IsNullOrWhiteSpace(paymentType))
            {
                // Intentar parsear el enum (case-insensitive)
                if (Enum.TryParse<PaymentType>(paymentType, true, out var parsed))
                {
                    paymentTypeEnum = parsed;
                    _logger.LogInformation("PaymentType parseado correctamente: {PaymentType}", paymentTypeEnum);
                    // #region agent log
                    try {
                        var logEntry = System.Text.Json.JsonSerializer.Serialize(new { location = "InvoicesController.cs:70", message = "PaymentType parsed successfully", data = new { originalValue = paymentType, parsedValue = paymentTypeEnum.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "C" }) + "\n";
                        SafeAppendToLogFile(logEntry);
                    } catch (Exception) { /* Ignore log errors */ }
                    // #endregion
                }
                else
                {
                    _logger.LogWarning("No se pudo parsear PaymentType: {PaymentType}", paymentType);
                    // #region agent log
                    try {
                        var logEntry = System.Text.Json.JsonSerializer.Serialize(new { location = "InvoicesController.cs:75", message = "PaymentType parse failed", data = new { originalValue = paymentType }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "C" }) + "\n";
                        SafeAppendToLogFile(logEntry);
                    } catch (Exception) { /* Ignore log errors */ }
                    // #endregion
                }
            }

            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { location = "InvoicesController.cs:79", message = "Calling GetFilteredAsync", data = new { year, quarter, supplierName, paymentTypeEnum = paymentTypeEnum?.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "D" }) + "\n";
                SafeAppendToLogFile(logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion

            var invoices = await _invoiceReceivedRepository.GetFilteredAsync(
                year,
                quarter,
                supplierName,
                paymentTypeEnum,
                cancellationToken);
            
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { location = "InvoicesController.cs:86", message = "GetFilteredAsync result", data = new { invoiceCount = invoices.Count() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "run1", hypothesisId = "D" }) + "\n";
                SafeAppendToLogFile(logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
            
            _logger.LogInformation("GetAllInvoices - Se encontraron {Count} facturas", invoices.Count());
            
            return Ok(invoices.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetAllInvoices");
            // #region agent log
            try {
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new { 
                    location = "InvoicesController.cs:113", 
                    message = "GetAllInvoices exception", 
                    data = new { 
                        errorMessage = ex.Message,
                        errorType = ex.GetType().Name,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    }, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
                    sessionId = "debug-session", 
                    runId = "run1", 
                    hypothesisId = "D" 
                }) + "\n";
                SafeAppendToLogFile(logEntry);
            } catch (Exception) { /* Ignore log errors */ }
            // #endregion
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene una factura recibida por ID con todos sus datos.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var allInvoices = await _getAllInvoicesUseCase.ExecuteAsync(cancellationToken);
            var invoice = allInvoices.FirstOrDefault(i => i.Id == id);
            
            if (invoice == null)
                return NotFound(new { message = $"Factura con ID {id} no encontrada." });
            
            return Ok(MapToDto(invoice));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mapea InvoiceReceived a DTO para la API.
    /// Expone todos los campos fiscales, económicos y de detalle según modelo contable.
    /// </summary>
    private static Dtos.InvoiceReceivedDto MapToDto(InvoiceReceived invoice)
    {
        return new Dtos.InvoiceReceivedDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            IssueDate = invoice.IssueDate,
            ReceivedDate = invoice.ReceivedDate,
            SupplierName = invoice.SupplierName,
            SupplierTaxId = invoice.SupplierTaxId,
            SupplierAddress = invoice.SupplierAddress,
            SubtotalAmount = invoice.SubtotalAmount.Value,
            TaxAmount = invoice.TaxAmount?.Value,
            TaxRate = invoice.TaxRate,
            TotalAmount = invoice.TotalAmount.Value,
            Currency = invoice.Currency,
            PaymentType = invoice.PaymentType.ToString(),
            Payments = invoice.Payments.Select(p => new Dtos.InvoiceReceivedPaymentDto
            {
                Id = p.Id,
                BankTransactionId = p.BankTransactionId,
                Amount = p.Amount.Value,
                Currency = p.Amount.Currency,
                PaymentDate = p.PaymentDate
            }).ToList(),
            Origin = invoice.Origin.ToString(),
            OriginalFilePath = invoice.OriginalFilePath,
            ProcessedFilePath = invoice.ProcessedFilePath,
            ExtractionConfidence = invoice.ExtractionConfidence,
            Notes = invoice.Notes,
            Lines = invoice.Lines.Select(line => new Dtos.InvoiceReceivedLineDto
            {
                Id = line.Id,
                LineNumber = line.LineNumber,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice.Value,
                UnitPriceCurrency = line.UnitPrice.Currency,
                TaxRate = line.TaxRate,
                LineTotal = line.LineTotal.Value,
                LineTotalCurrency = line.LineTotal.Currency
            }).ToList(),
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }

    /// <summary>
    /// Sube una factura en formato PDF.
    /// Devuelve la factura completa con todos sus datos fiscales y de detalle.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadInvoice([FromForm] UploadInvoiceDto dto, CancellationToken cancellationToken)
    {
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest("No se proporcionó ningún archivo.");

        var command = new UploadInvoiceCommand(
            dto.File.OpenReadStream(),
            dto.File.FileName,
            dto.File.ContentType);

        var result = await _uploadInvoiceUseCase.ExecuteAsync(command, cancellationToken);

        // Obtener la factura completa con todos sus datos
        var allInvoices = await _getAllInvoicesUseCase.ExecuteAsync(cancellationToken);
        var invoice = allInvoices.FirstOrDefault(i => i.Id == result.InvoiceId);
        
        if (invoice == null)
            return StatusCode(500, new { error = "La factura se subió pero no se pudo recuperar." });

        return Ok(MapToDto(invoice));
    }

    /// <summary>
    /// Actualiza cualquier campo de una factura recibida.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateInvoiceCommand(
                id,
                dto.InvoiceNumber,
                dto.SupplierName,
                dto.SupplierTaxId,
                dto.IssueDate,
                dto.DueDate,
                dto.TotalAmount,
                dto.TaxAmount,
                dto.SubtotalAmount,
                dto.Currency,
                dto.Notes);

            await _updateInvoiceUseCase.ExecuteAsync(command, cancellationToken);

            // Obtener la factura actualizada
            var allInvoices = await _getAllInvoicesUseCase.ExecuteAsync(cancellationToken);
            var invoice = allInvoices.FirstOrDefault(i => i.Id == id);
            
            if (invoice == null)
                return NotFound(new { message = $"Factura con ID {id} no encontrada." });

            return Ok(MapToDto(invoice));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza el nombre del proveedor de una factura (método legacy, usar PUT /{id} en su lugar).
    /// </summary>
    [HttpPatch("{id:guid}/supplier")]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] UpdateSupplierDto dto, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierName))
                return BadRequest(new { error = "El nombre del proveedor no puede estar vacío." });

            var command = new UpdateInvoiceSupplierCommand(id, dto.SupplierName);
            await _updateInvoiceSupplierUseCase.ExecuteAsync(command, cancellationToken);

            // Obtener la factura actualizada
            var allInvoices = await _getAllInvoicesUseCase.ExecuteAsync(cancellationToken);
            var invoice = allInvoices.FirstOrDefault(i => i.Id == id);
            
            if (invoice == null)
                return NotFound(new { message = $"Factura con ID {id} no encontrada." });

            return Ok(MapToDto(invoice));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Marca una factura como pagada.
    /// </summary>
    [HttpPost("{id}/mark-paid")]
    public async Task<IActionResult> MarkAsPaid(Guid id, [FromBody] MarkAsPaidDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementar caso de uso
        return NoContent();
    }

    /// <summary>
    /// Elimina una factura recibida.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteInvoice(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _deleteInvoiceUseCase.ExecuteAsync(id, cancellationToken);
            return Ok(new { message = "Factura eliminada correctamente" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exporta facturas recibidas a Excel con los filtros aplicados.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportToExcel(
        [FromQuery] int? year,
        [FromQuery] int? quarter,
        [FromQuery] string? supplierName,
        [FromQuery] string? paymentType,
        CancellationToken cancellationToken)
    {
        try
        {
            PaymentType? paymentTypeEnum = null;
            if (!string.IsNullOrWhiteSpace(paymentType) && Enum.TryParse<PaymentType>(paymentType, true, out var parsed))
            {
                paymentTypeEnum = parsed;
            }

            var invoices = await _invoiceReceivedRepository.GetFilteredAsync(
                year,
                quarter,
                supplierName,
                paymentTypeEnum,
                cancellationToken);

            var loggerFactory = HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var excelLogger = loggerFactory.CreateLogger<InvoiceReceivedExcelExporter>();
            var exporter = new InvoiceReceivedExcelExporter(excelLogger);
            
            var excelContent = await exporter.ExportToExcelAsync(invoices.ToList(), cancellationToken);

            var fileName = $"FacturasRecibidas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Chat con OpenAI para análisis fiscal de facturas recibidas.
    /// Funciona como ChatGPT pero con acceso a los datos de las facturas.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> ChatAboutInvoices(
        [FromBody] InvoiceChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtener facturas según filtros
            PaymentType? paymentTypeEnum = null;
            if (!string.IsNullOrWhiteSpace(request.PaymentType) && Enum.TryParse<PaymentType>(request.PaymentType, true, out var parsed))
            {
                paymentTypeEnum = parsed;
            }

            var invoices = await _invoiceReceivedRepository.GetFilteredAsync(
                request.Year,
                request.Quarter,
                request.SupplierName,
                paymentTypeEnum,
                cancellationToken);

            var invoiceList = invoices.ToList();

            // Preparar contexto estructurado para OpenAI
            var context = PrepareChatContext(invoiceList);
            var contextJson = System.Text.Json.JsonSerializer.Serialize(context, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            // Crear el prompt del sistema con el contexto de las facturas
            var systemPrompt = $@"Eres un asistente financiero experto que ayuda a analizar facturas de proveedores. 
Tienes acceso a los siguientes datos de facturas del usuario:

RESUMEN DE FACTURAS:
- Total de facturas: {invoiceList.Count}
- Importe total: {invoiceList.Sum(i => i.TotalAmount.Value):N2} EUR
- Total IVA: {invoiceList.Sum(i => i.TaxAmount?.Value ?? 0):N2} EUR
- Total Base Imponible: {invoiceList.Sum(i => i.SubtotalAmount.Value):N2} EUR

DATOS DETALLADOS:
{contextJson}

LISTA DE FACTURAS INDIVIDUALES:
{string.Join("\n", invoiceList.Select(i => $"- {i.InvoiceNumber}: {i.SupplierName} | {i.TotalAmount.Value:N2} {i.TotalAmount.Currency} | Fecha: {i.IssueDate:dd/MM/yyyy} | Tipo pago: {i.PaymentType}"))}

INSTRUCCIONES:
1. Responde siempre en español
2. Sé conciso pero informativo
3. Usa los datos que tienes para dar respuestas precisas
4. Si te preguntan por proveedores, importes, fechas, etc., usa los datos reales
5. Puedes hacer cálculos, comparaciones, estadísticas, rankings, etc.
6. Si no tienes datos suficientes para responder algo, dilo claramente";

            // Llamar a OpenAI con el contexto
            var answer = await _openAIClient.GetChatCompletionAsync(
                systemPrompt,
                request.Question,
                cancellationToken);

            var response = new
            {
                answer = answer,
                context = context
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en chat de facturas");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private static readonly object _logFileLock = new object();

    private static void SafeAppendToLogFile(string logEntry)
    {
        const string logFilePath = @"c:\repo\FresiaFlow\.cursor\debug.log";
        lock (_logFileLock)
        {
            try
            {
                using (var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(logEntry.TrimEnd('\n', '\r'));
                }
            }
            catch
            {
                // Ignorar errores de logging para no afectar la funcionalidad principal
            }
        }
    }

    private static object PrepareChatContext(List<InvoiceReceived> invoices)
    {
        return new
        {
            totalInvoices = invoices.Count,
            totalAmount = invoices.Sum(i => i.TotalAmount.Value),
            totalTax = invoices.Sum(i => i.TaxAmount?.Value ?? 0),
            totalSubtotal = invoices.Sum(i => i.SubtotalAmount.Value),
            bySupplier = invoices.GroupBy(i => i.SupplierName).Select(g => new
            {
                supplier = g.Key,
                count = g.Count(),
                total = g.Sum(i => i.TotalAmount.Value)
            }).OrderByDescending(x => x.total).ToList(),
            byPaymentType = invoices.GroupBy(i => i.PaymentType).Select(g => new
            {
                paymentType = g.Key.ToString(),
                count = g.Count(),
                total = g.Sum(i => i.TotalAmount.Value)
            }).ToList(),
            byMonth = invoices.GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month }).Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                count = g.Count(),
                total = g.Sum(i => i.TotalAmount.Value),
                tax = g.Sum(i => i.TaxAmount?.Value ?? 0)
            }).OrderBy(x => x.year).ThenBy(x => x.month).ToList()
        };
    }
}

/// <summary>
/// Request para chat sobre facturas.
/// </summary>
public class InvoiceChatRequest
{
    public string Question { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int? Quarter { get; set; }
    public string? SupplierName { get; set; }
    public string? PaymentType { get; set; }
}

