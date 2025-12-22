using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para exportar facturas emitidas a Excel por período.
/// </summary>
public class ExportIssuedInvoicesUseCase : IExportIssuedInvoicesUseCase
{
    private readonly IIssuedInvoiceRepository _repository;
    private readonly IExcelExporter _excelExporter;
    private readonly ILogger<ExportIssuedInvoicesUseCase> _logger;

    public ExportIssuedInvoicesUseCase(
        IIssuedInvoiceRepository repository,
        IExcelExporter excelExporter,
        ILogger<ExportIssuedInvoicesUseCase> logger)
    {
        _repository = repository;
        _excelExporter = excelExporter;
        _logger = logger;
    }

    public async Task<ExportIssuedInvoicesResult> ExecuteAsync(
        ExportIssuedInvoicesCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando exportación de facturas emitidas");

        // Calcular rango de fechas según los parámetros
        var (startDate, endDate) = CalculateDateRange(command);

        _logger.LogDebug("Exportando facturas desde {StartDate} hasta {EndDate}", startDate, endDate);

        // Obtener facturas del período
        var invoices = await _repository.GetByDateRangeAsync(startDate, endDate, cancellationToken);

        if (invoices.Count == 0)
        {
            _logger.LogWarning("No se encontraron facturas en el período especificado");
            throw new InvalidOperationException("No hay facturas para exportar en el período especificado.");
        }

        // Exportar a Excel
        var excelContent = await _excelExporter.ExportToExcelAsync(invoices, cancellationToken);

        // Generar nombre de archivo
        var fileName = GenerateFileName(command, startDate, endDate);

        _logger.LogInformation("Exportadas {Count} facturas a {FileName}", invoices.Count, fileName);

        return new ExportIssuedInvoicesResult(
            excelContent,
            fileName,
            invoices.Count);
    }

    private (DateTime startDate, DateTime endDate) CalculateDateRange(ExportIssuedInvoicesCommand command)
    {
        // Si se proporciona rango explícito, usarlo
        if (command.StartDate.HasValue && command.EndDate.HasValue)
        {
            return (command.StartDate.Value.Date, command.EndDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        // Si se proporciona año y trimestre
        if (command.Year.HasValue && command.Quarter.HasValue)
        {
            var quarter = command.Quarter.Value;
            if (quarter < 1 || quarter > 4)
                throw new ArgumentException("El trimestre debe estar entre 1 y 4", nameof(command));

            var startMonth = (quarter - 1) * 3 + 1;
            var startDate = new DateTime(command.Year.Value, startMonth, 1);
            var endDate = startDate.AddMonths(3).AddDays(-1);

            return (startDate, endDate.Date.AddDays(1).AddTicks(-1));
        }

        // Si se proporciona año y mes
        if (command.Year.HasValue && command.Month.HasValue)
        {
            var month = command.Month.Value;
            if (month < 1 || month > 12)
                throw new ArgumentException("El mes debe estar entre 1 y 12", nameof(command));

            var startDate = new DateTime(command.Year.Value, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return (startDate, endDate.Date.AddDays(1).AddTicks(-1));
        }

        // Si solo se proporciona año
        if (command.Year.HasValue)
        {
            var startDate = new DateTime(command.Year.Value, 1, 1);
            var endDate = new DateTime(command.Year.Value, 12, 31);

            return (startDate, endDate.Date.AddDays(1).AddTicks(-1));
        }

        // Por defecto: último mes
        var defaultEndDate = DateTime.Today;
        var defaultStartDate = defaultEndDate.AddMonths(-1);

        return (defaultStartDate, defaultEndDate.Date.AddDays(1).AddTicks(-1));
    }

    private string GenerateFileName(ExportIssuedInvoicesCommand command, DateTime startDate, DateTime endDate)
    {
        if (command.Year.HasValue && command.Quarter.HasValue)
        {
            return $"Facturas_Emitidas_{command.Year}_T{command.Quarter}_{DateTime.Now:yyyyMMdd}.xlsx";
        }

        if (command.Year.HasValue && command.Month.HasValue)
        {
            var monthName = new DateTime(command.Year.Value, command.Month.Value, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
            return $"Facturas_Emitidas_{command.Year}_{monthName}_{DateTime.Now:yyyyMMdd}.xlsx";
        }

        if (command.Year.HasValue)
        {
            return $"Facturas_Emitidas_{command.Year}_{DateTime.Now:yyyyMMdd}.xlsx";
        }

        return $"Facturas_Emitidas_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
    }
}

