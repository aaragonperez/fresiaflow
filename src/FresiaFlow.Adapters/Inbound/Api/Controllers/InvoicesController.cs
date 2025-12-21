using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Adapters.Inbound.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para gestión de facturas.
/// Solo delega a casos de uso; no contiene lógica de negocio.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IUploadInvoiceUseCase _uploadInvoiceUseCase;
    private readonly IGetAllInvoicesUseCase _getAllInvoicesUseCase;

    public InvoicesController(
        IUploadInvoiceUseCase uploadInvoiceUseCase,
        IGetAllInvoicesUseCase getAllInvoicesUseCase)
    {
        _uploadInvoiceUseCase = uploadInvoiceUseCase;
        _getAllInvoicesUseCase = getAllInvoicesUseCase;
    }

    /// <summary>
    /// Obtiene todas las facturas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllInvoices(CancellationToken cancellationToken)
    {
        try
        {
            var invoices = await _getAllInvoicesUseCase.ExecuteAsync(cancellationToken);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene una factura por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        // TODO: Implementar
        return NotFound();
    }

    /// <summary>
    /// Sube una factura en formato PDF.
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

        return Ok(result);
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
    /// Elimina una factura.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvoice(Guid id, CancellationToken cancellationToken)
    {
        // TODO: Implementar caso de uso
        return NoContent();
    }
}

