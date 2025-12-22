using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Invoices;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para importar facturas emitidas desde un archivo Excel.
/// </summary>
public class ImportIssuedInvoicesFromExcelUseCase : IImportIssuedInvoicesFromExcelUseCase
{
    private readonly IExcelProcessor _excelProcessor;
    private readonly IIssuedInvoiceRepository _repository;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<ImportIssuedInvoicesFromExcelUseCase> _logger;

    public ImportIssuedInvoicesFromExcelUseCase(
        IExcelProcessor excelProcessor,
        IIssuedInvoiceRepository repository,
        IFileStorage fileStorage,
        ILogger<ImportIssuedInvoicesFromExcelUseCase> logger)
    {
        _excelProcessor = excelProcessor;
        _repository = repository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<ImportIssuedInvoicesResult> ExecuteAsync(
        ImportIssuedInvoicesFromExcelCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando importación de facturas desde Excel: {FileName}", command.FileName);

        try
        {
            // 1. Guardar archivo
            var filePath = await _fileStorage.SaveFileAsync(
                command.FileStream,
                command.FileName,
                command.ContentType,
                cancellationToken);

            _logger.LogDebug("Archivo guardado en: {FilePath}", filePath);

            // 2. Procesar Excel
            var invoiceData = await _excelProcessor.ProcessExcelAsync(filePath, cancellationToken);

            if (invoiceData.Count == 0)
            {
                _logger.LogWarning("No se encontraron facturas en el archivo Excel");
                return new ImportIssuedInvoicesResult(0, 0, 0, "No se encontraron facturas en el archivo.");
            }

            _logger.LogInformation("Procesadas {Count} facturas del Excel", invoiceData.Count);

            // 3. Convertir a entidades de dominio
            var invoices = new List<IssuedInvoice>();
            int duplicates = 0;
            int errors = 0;

            foreach (var data in invoiceData)
            {
                try
                {
                    // Verificar si ya existe
                    var existing = await _repository.GetByInvoiceNumberAsync(
                        data.Series,
                        data.InvoiceNumber,
                        cancellationToken);

                    if (existing != null)
                    {
                        _logger.LogDebug("Factura duplicada: {Series}-{Number}", data.Series, data.InvoiceNumber);
                        duplicates++;
                        continue;
                    }

                    // Crear nueva factura
                    var invoice = new IssuedInvoice(
                        data.Series,
                        data.InvoiceNumber,
                        data.IssueDate,
                        data.TaxableBase,
                        data.TaxAmount,
                        data.TotalAmount,
                        data.ClientId,
                        data.ClientTaxId,
                        data.ClientName,
                        data.Address,
                        data.City,
                        data.PostalCode,
                        data.Province,
                        data.Country,
                        filePath);

                    invoices.Add(invoice);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando factura: {InvoiceNumber}", data.InvoiceNumber);
                    errors++;
                }
            }

            // 4. Guardar en lote
            if (invoices.Count > 0)
            {
                await _repository.AddRangeAsync(invoices, cancellationToken);
                _logger.LogInformation("Guardadas {Count} facturas nuevas", invoices.Count);
            }

            return new ImportIssuedInvoicesResult(
                invoices.Count,
                duplicates,
                errors,
                $"Importadas {invoices.Count} facturas. {duplicates} duplicadas. {errors} errores.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importando facturas desde Excel");
            throw new InvalidOperationException($"Error al importar facturas: {ex.Message}", ex);
        }
    }
}

