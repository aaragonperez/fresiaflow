using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para gestión de facturas recibidas procesadas automáticamente.
/// </summary>
[ApiController]
[Route("api/invoices/received")]
public class InvoiceReceivedController : ControllerBase
{
    private readonly IInvoiceReceivedRepository _repository;
    private readonly IProcessIncomingInvoiceCommandHandler _processHandler;
    private readonly IMarkInvoiceAsReviewedUseCase _markAsReviewedUseCase;
    private readonly ILogger<InvoiceReceivedController> _logger;

    public InvoiceReceivedController(
        IInvoiceReceivedRepository repository,
        IProcessIncomingInvoiceCommandHandler processHandler,
        IMarkInvoiceAsReviewedUseCase markAsReviewedUseCase,
        ILogger<InvoiceReceivedController> logger)
    {
        _repository = repository;
        _processHandler = processHandler;
        _markAsReviewedUseCase = markAsReviewedUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las facturas recibidas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var invoices = await _repository.GetAllAsync(cancellationToken);
        return Ok(invoices.Select(MapToDto));
    }

    /// <summary>
    /// Obtiene una factura recibida por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(id, cancellationToken);

        if (invoice == null)
            return NotFound(new { message = $"Factura con ID {id} no encontrada." });

        return Ok(MapToDto(invoice));
    }

    /// <summary>
    /// Obtiene facturas por estado.
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(
        InvoiceReceivedStatus status,
        CancellationToken cancellationToken)
    {
        var invoices = await _repository.GetByStatusAsync(status, cancellationToken);
        return Ok(invoices.Select(MapToDto));
    }

    /// <summary>
    /// Busca una factura por número.
    /// </summary>
    [HttpGet("by-number/{invoiceNumber}")]
    public async Task<IActionResult> GetByInvoiceNumber(
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);

        if (invoice == null)
            return NotFound(new { message = $"Factura {invoiceNumber} no encontrada." });

        return Ok(MapToDto(invoice));
    }

    /// <summary>
    /// Reprocesa una factura desde un archivo PDF.
    /// </summary>
    [HttpPost("reprocess")]
    public async Task<IActionResult> ReprocessInvoice(
        [FromBody] ReprocessInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return BadRequest(new { message = "La ruta del archivo es obligatoria." });

        if (!System.IO.File.Exists(request.FilePath))
            return BadRequest(new { message = "El archivo especificado no existe." });

        try
        {
            var command = new ProcessIncomingInvoiceCommand(request.FilePath);
            var invoiceId = await _processHandler.HandleAsync(command, cancellationToken);

            var invoice = await _repository.GetByIdAsync(invoiceId, cancellationToken);

            return Ok(new
            {
                message = "Factura reprocesada exitosamente",
                invoiceId,
                invoice = MapToDto(invoice!)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocesando factura desde {FilePath}", request.FilePath);
            return StatusCode(500, new { message = "Error procesando la factura", error = ex.Message });
        }
    }

    /// <summary>
    /// Marca una factura como revisada.
    /// </summary>
    [HttpPost("{id:guid}/mark-reviewed")]
    public async Task<IActionResult> MarkAsReviewed(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _markAsReviewedUseCase.ExecuteAsync(id, cancellationToken);
            return Ok(new { message = "Factura marcada como revisada" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marcando factura {InvoiceId} como revisada", id);
            return StatusCode(500, new { message = "Error procesando la solicitud", error = ex.Message });
        }
    }

    private static object MapToDto(InvoiceReceived invoice)
    {
        return new
        {
            id = invoice.Id,
            invoiceNumber = invoice.InvoiceNumber,
            supplierName = invoice.SupplierName,
            supplierTaxId = invoice.SupplierTaxId,
            issueDate = invoice.IssueDate,
            dueDate = invoice.DueDate,
            totalAmount = invoice.TotalAmount.Value,
            taxAmount = invoice.TaxAmount?.Value,
            subtotalAmount = invoice.SubtotalAmount?.Value,
            currency = invoice.Currency,
            originalFilePath = invoice.OriginalFilePath,
            processedFilePath = invoice.ProcessedFilePath,
            processedAt = invoice.ProcessedAt,
            status = invoice.Status.ToString(),
            notes = invoice.Notes,
            linesCount = invoice.Lines.Count,
            lines = invoice.Lines.Select(line => new
            {
                id = line.Id,
                lineNumber = line.LineNumber,
                description = line.Description,
                quantity = line.Quantity,
                unitPrice = line.UnitPrice.Value,
                taxRate = line.TaxRate,
                lineTotal = line.LineTotal.Value
            })
        };
    }
}

/// <summary>
/// Request para reprocesar una factura.
/// </summary>
public class ReprocessInvoiceRequest
{
    public string FilePath { get; set; } = string.Empty;
}

