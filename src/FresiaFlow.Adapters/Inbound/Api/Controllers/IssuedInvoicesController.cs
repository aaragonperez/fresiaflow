using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Adapters.Inbound.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para gestión de facturas emitidas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IssuedInvoicesController : ControllerBase
{
    private readonly IImportIssuedInvoicesFromExcelUseCase _importUseCase;
    private readonly IExportIssuedInvoicesUseCase _exportUseCase;

    public IssuedInvoicesController(
        IImportIssuedInvoicesFromExcelUseCase importUseCase,
        IExportIssuedInvoicesUseCase exportUseCase)
    {
        _importUseCase = importUseCase;
        _exportUseCase = exportUseCase;
    }

    /// <summary>
    /// Importa facturas emitidas desde un archivo Excel.
    /// </summary>
    [HttpPost("import-excel")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportFromExcel([FromForm] UploadExcelDto dto, CancellationToken cancellationToken)
    {
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest("No se proporcionó ningún archivo.");

        if (!dto.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !dto.File.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("El archivo debe ser un Excel (.xlsx o .xls)");
        }

        try
        {
            var command = new ImportIssuedInvoicesFromExcelCommand(
                dto.File.OpenReadStream(),
                dto.File.FileName,
                dto.File.ContentType);

            var result = await _importUseCase.ExecuteAsync(command, cancellationToken);

            return Ok(new
            {
                success = true,
                imported = result.ImportedCount,
                duplicates = result.DuplicatesCount,
                errors = result.ErrorsCount,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exporta facturas emitidas a Excel por período.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportToExcel(
        [FromQuery] int? year,
        [FromQuery] int? quarter,
        [FromQuery] int? month,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ExportIssuedInvoicesCommand(
                startDate,
                endDate,
                year,
                quarter,
                month);

            var result = await _exportUseCase.ExecuteAsync(command, cancellationToken);

            return File(
                result.ExcelContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

