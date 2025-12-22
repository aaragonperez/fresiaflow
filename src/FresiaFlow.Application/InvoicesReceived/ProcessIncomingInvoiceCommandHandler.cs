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

            // 4. Convertir fechas a UTC para PostgreSQL
            var issueDate = EnsureUtc(extractedData.GetIssueDate());
            var dueDate = extractedData.GetDueDate();
            var dueDateUtc = dueDate.HasValue ? EnsureUtc(dueDate.Value) : (DateTime?)null;

            // 5. Crear entidad de dominio
            var currency = string.IsNullOrWhiteSpace(extractedData.Currency) ? "EUR" : extractedData.Currency;
            var receivedDate = DateTime.UtcNow;
            
            // Calcular base imponible: si no viene, calcularla restando IVA del total
            var subtotalAmount = extractedData.SubtotalAmount ?? 
                (extractedData.TaxAmount.HasValue 
                    ? extractedData.TotalAmount - extractedData.TaxAmount.Value 
                    : extractedData.TotalAmount);
            
            var invoice = new InvoiceReceived(
                extractedData.InvoiceNumber,
                extractedData.SupplierName,
                issueDate,
                receivedDate,
                new Money(subtotalAmount, currency),
                new Money(extractedData.TotalAmount, currency),
                currency,
                InvoiceOrigin.Email,
                command.FilePath);

            // Configurar campos opcionales
            if (!string.IsNullOrWhiteSpace(extractedData.SupplierTaxId))
                invoice.SetSupplierTaxId(extractedData.SupplierTaxId);

            if (extractedData.TaxAmount.HasValue)
            {
                invoice.SetTaxAmount(new Money(extractedData.TaxAmount.Value, currency));
                // Calcular tipo de IVA si tenemos base e IVA
                if (subtotalAmount > 0)
                {
                    var taxRate = (extractedData.TaxAmount.Value / subtotalAmount) * 100m;
                    invoice.SetTaxRate(taxRate);
                }
            }

            // La base imponible ya se estableció en el constructor, pero si viene explícitamente la actualizamos
            if (extractedData.SubtotalAmount.HasValue && extractedData.SubtotalAmount.Value != subtotalAmount)
            {
                invoice.SetSubtotalAmount(new Money(extractedData.SubtotalAmount.Value, currency));
            }

            // Agregar líneas de detalle
            foreach (var lineDto in extractedData.Lines.OrderBy(l => l.LineNumber))
            {
                var line = new InvoiceReceivedLine(
                    lineDto.LineNumber,
                    lineDto.Description,
                    lineDto.Quantity,
                    new Money(lineDto.UnitPrice, currency),
                    new Money(lineDto.LineTotal, currency));

                if (lineDto.TaxRate.HasValue)
                    line.SetTaxRate(lineDto.TaxRate);

                invoice.AddLine(line);
            }

            // 6. Persistir
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

    /// <summary>
    /// Asegura que una fecha esté en UTC para PostgreSQL.
    /// </summary>
    private static DateTime EnsureUtc(DateTime date)
    {
        return date.Kind == DateTimeKind.Utc 
            ? date 
            : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

