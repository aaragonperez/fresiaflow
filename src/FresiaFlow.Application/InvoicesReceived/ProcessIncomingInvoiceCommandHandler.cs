using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Handler que procesa facturas entrantes desde archivos PDF.
/// Orquesta la extracción de texto, análisis con IA y persistencia.
/// </summary>
public class ProcessIncomingInvoiceCommandHandler : IProcessIncomingInvoiceCommandHandler
{
    private readonly IPdfTextExtractorService _pdfExtractor;
    private readonly IInvoiceExtractionService _invoiceExtraction;
    private readonly IInvoiceReceivedRepository _repository;
    private readonly ILogger<ProcessIncomingInvoiceCommandHandler> _logger;

    public ProcessIncomingInvoiceCommandHandler(
        IPdfTextExtractorService pdfExtractor,
        IInvoiceExtractionService invoiceExtraction,
        IInvoiceReceivedRepository repository,
        ILogger<ProcessIncomingInvoiceCommandHandler> logger)
    {
        _pdfExtractor = pdfExtractor;
        _invoiceExtraction = invoiceExtraction;
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(
        ProcessIncomingInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando procesamiento de factura: {FilePath}", command.FilePath);

        try
        {
            // 1. Extraer texto del PDF
            _logger.LogDebug("Extrayendo texto del PDF...");
            var text = await _pdfExtractor.ExtractTextAsync(command.FilePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("No se pudo extraer texto del PDF.");
            }

            // 2. Extraer datos semánticos con IA
            _logger.LogDebug("Analizando factura con IA...");
            var extractedData = await _invoiceExtraction.ExtractInvoiceDataAsync(text, cancellationToken);

            // 3. Verificar si ya existe la factura
            var existing = await _repository.GetByInvoiceNumberAsync(
                extractedData.InvoiceNumber, 
                cancellationToken);

            if (existing != null)
            {
                _logger.LogWarning(
                    "La factura {InvoiceNumber} ya existe en el sistema.",
                    extractedData.InvoiceNumber);
                throw new InvalidOperationException(
                    $"La factura {extractedData.InvoiceNumber} ya fue procesada anteriormente.");
            }

            // 4. Crear entidad de dominio
            var invoice = new InvoiceReceived(
                extractedData.InvoiceNumber,
                extractedData.SupplierName,
                extractedData.IssueDate,
                new Money(extractedData.TotalAmount, extractedData.Currency),
                extractedData.Currency,
                command.FilePath);

            // Configurar campos opcionales
            if (!string.IsNullOrWhiteSpace(extractedData.SupplierTaxId))
                invoice.SetSupplierTaxId(extractedData.SupplierTaxId);

            if (extractedData.DueDate.HasValue)
                invoice.SetDueDate(extractedData.DueDate);

            if (extractedData.TaxAmount.HasValue)
                invoice.SetTaxAmount(new Money(extractedData.TaxAmount.Value, extractedData.Currency));

            if (extractedData.SubtotalAmount.HasValue)
                invoice.SetSubtotalAmount(new Money(extractedData.SubtotalAmount.Value, extractedData.Currency));

            // Agregar líneas de detalle
            foreach (var lineDto in extractedData.Lines.OrderBy(l => l.LineNumber))
            {
                var line = new InvoiceReceivedLine(
                    lineDto.LineNumber,
                    lineDto.Description,
                    lineDto.Quantity,
                    new Money(lineDto.UnitPrice, extractedData.Currency),
                    new Money(lineDto.LineTotal, extractedData.Currency));

                if (lineDto.TaxRate.HasValue)
                    line.SetTaxRate(lineDto.TaxRate);

                invoice.AddLine(line);
            }

            // 5. Persistir
            _logger.LogDebug("Guardando factura en la base de datos...");
            await _repository.AddAsync(invoice, cancellationToken);

            _logger.LogInformation(
                "Factura {InvoiceNumber} procesada exitosamente con ID {Id}",
                extractedData.InvoiceNumber,
                invoice.Id);

            return invoice.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error procesando factura desde archivo {FilePath}",
                command.FilePath);
            throw;
        }
    }
}

