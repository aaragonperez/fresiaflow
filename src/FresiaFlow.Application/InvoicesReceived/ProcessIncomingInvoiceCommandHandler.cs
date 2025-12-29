using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Handler que procesa facturas entrantes desde archivos PDF.
/// Orquesta la extracción de texto, análisis con IA y persistencia.
/// </summary>
public class ProcessIncomingInvoiceCommandHandler : IProcessIncomingInvoiceCommandHandler
{
    private const string ValidationSeparator = "||";
    private static readonly JsonSerializerOptions SnapshotSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IPdfTextExtractorService _ocrService;
    private readonly IDocumentClassificationService _documentClassifier;
    private readonly IInvoiceExtractionService _invoiceExtraction;
    private readonly IInvoiceProcessingSnapshotRepository _snapshotRepository;
    private readonly IInvoiceReceivedRepository _repository;
    private readonly InvoiceProcessingOptions _processingOptions;
    private readonly ILogger<ProcessIncomingInvoiceCommandHandler> _logger;

    public ProcessIncomingInvoiceCommandHandler(
        IPdfTextExtractorService ocrService,
        IDocumentClassificationService documentClassifier,
        IInvoiceExtractionService invoiceExtraction,
        IInvoiceProcessingSnapshotRepository snapshotRepository,
        IInvoiceReceivedRepository repository,
        IOptions<InvoiceProcessingOptions> processingOptions,
        ILogger<ProcessIncomingInvoiceCommandHandler> logger)
    {
        _ocrService = ocrService;
        _documentClassifier = documentClassifier;
        _invoiceExtraction = invoiceExtraction;
        _snapshotRepository = snapshotRepository;
        _repository = repository;
        _processingOptions = processingOptions.Value;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(
        ProcessIncomingInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando procesamiento de factura: {FilePath}", command.FilePath);

        try
        {
            var fileHash = await ComputeFileHashAsync(command.FilePath, cancellationToken);
            var snapshot = await GetOrCreateSnapshotAsync(command.FilePath, fileHash, cancellationToken);

            var ocrResult = await EnsureOcrAsync(snapshot, command.FilePath, cancellationToken);
            var classification = await EnsureClassificationAsync(snapshot, ocrResult, cancellationToken);

            if (!classification.IsInvoice)
            {
                _logger.LogWarning(
                    "Documento clasificado como {DocumentType} ({Language}). " +
                    "Se procesa igualmente para mantener compatibilidad.",
                    classification.DocumentType,
                    classification.Language);
            }

            var extraction = await EnsureExtractionAsync(snapshot, ocrResult, useHighPrecision: false, cancellationToken);
            var validation = await EnsureValidationAsync(snapshot, extraction, cancellationToken);

            if (!snapshot.FallbackTriggered && ShouldTriggerFallback(ocrResult, validation))
            {
                _logger.LogWarning(
                    "Activando fallback premium para {FilePath} (confidence {Confidence}, status {Status})",
                    command.FilePath,
                    ocrResult.Confidence,
                    validation.Status);

                extraction = await EnsureExtractionAsync(snapshot, ocrResult, useHighPrecision: true, cancellationToken);
                validation = await EnsureValidationAsync(snapshot, extraction, cancellationToken, force: true);
                snapshot.MarkFallback(BuildFallbackReason(ocrResult, validation));
                await _snapshotRepository.UpdateAsync(snapshot, cancellationToken);
            }

            var invoiceId = await PersistInvoiceAsync(
                extraction,
                command.FilePath,
                ocrResult.Confidence,
                validation,
                cancellationToken);

            _logger.LogInformation(
                "Factura {InvoiceNumber} procesada exitosamente con snapshot {SnapshotId}",
                extraction.InvoiceNumber,
                snapshot.Id);

            return invoiceId;
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

    private async Task<InvoiceProcessingSnapshot> GetOrCreateSnapshotAsync(
        string filePath,
        string fileHash,
        CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotRepository.GetBySourceFileHashAsync(fileHash, cancellationToken)
                      ?? await _snapshotRepository.GetBySourceFilePathAsync(filePath, cancellationToken);

        if (snapshot == null)
        {
            snapshot = new InvoiceProcessingSnapshot(filePath, fileHash);
            await _snapshotRepository.AddAsync(snapshot, cancellationToken);
            return snapshot;
        }

        if (!string.Equals(snapshot.SourceFileHash, fileHash, StringComparison.OrdinalIgnoreCase))
        {
            var freshSnapshot = new InvoiceProcessingSnapshot(filePath, fileHash);
            await _snapshotRepository.AddAsync(freshSnapshot, cancellationToken);
            return freshSnapshot;
        }

        return snapshot;
    }

    private async Task<OcrResultDto> EnsureOcrAsync(
        InvoiceProcessingSnapshot snapshot,
        string filePath,
        CancellationToken cancellationToken)
    {
        var cached = snapshot.ToOcrResult();
        if (cached != null)
        {
            return cached;
        }

        var result = await _ocrService.ExtractAsync(filePath, cancellationToken);
        snapshot.SaveOcrPayload(
            result.Text,
            result.Confidence,
            JsonSerializer.Serialize(result.Pages, SnapshotSerializerOptions));
        await _snapshotRepository.UpdateAsync(snapshot, cancellationToken);

        return result;
    }

    private async Task<DocumentClassificationResultDto> EnsureClassificationAsync(
        InvoiceProcessingSnapshot snapshot,
        OcrResultDto ocrResult,
        CancellationToken cancellationToken)
    {
        var cached = snapshot.ToClassificationResult();
        if (cached != null)
        {
            return cached;
        }

        var result = await _documentClassifier.ClassifyAsync(ocrResult, cancellationToken);
        snapshot.SaveClassificationPayload(
            result.DocumentType,
            result.Language,
            result.SupplierGuess,
            result.Confidence,
            result.RawJson ?? JsonSerializer.Serialize(result, SnapshotSerializerOptions));
        await _snapshotRepository.UpdateAsync(snapshot, cancellationToken);

        return result;
    }

    private async Task<InvoiceExtractionResultDto> EnsureExtractionAsync(
        InvoiceProcessingSnapshot snapshot,
        OcrResultDto ocrResult,
        bool useHighPrecision,
        CancellationToken cancellationToken)
    {
        var cached = snapshot.ToExtractionResult();
        if (cached != null)
        {
            var canReuse = !useHighPrecision || snapshot.FallbackTriggered;
            if (canReuse)
            {
                return cached;
            }
        }

        var request = new InvoiceExtractionRequest(ocrResult.Text, useHighPrecision);
        var result = await _invoiceExtraction.ExtractInvoiceDataAsync(request, cancellationToken);

        var json = JsonSerializer.Serialize(result, SnapshotSerializerOptions);
        var hash = ComputeSha256(json);

        snapshot.SaveExtractionPayload(
            json,
            _processingOptions.ExtractionVersion,
            hash,
            extractionConfidence: ocrResult.Confidence);
        await _snapshotRepository.UpdateAsync(snapshot, cancellationToken);

        return result;
    }

    private async Task<InvoiceValidationResult> EnsureValidationAsync(
        InvoiceProcessingSnapshot snapshot,
        InvoiceExtractionResultDto extraction,
        CancellationToken cancellationToken,
        bool force = false)
    {
        if (!force && snapshot.ValidationStatus != InvoiceValidationStatus.Pending)
        {
            var errors = string.IsNullOrWhiteSpace(snapshot.ValidationErrors)
                ? Array.Empty<string>()
                : snapshot.ValidationErrors.Split(
                    ValidationSeparator,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new InvoiceValidationResult(snapshot.ValidationStatus, errors);
        }

        var validation = InvoiceValidationEngine.Validate(extraction, _processingOptions);
        var serializedErrors = validation.Errors.Any()
            ? string.Join(ValidationSeparator, validation.Errors)
            : null;

        snapshot.SaveValidationResult(validation.Status, serializedErrors);
        await _snapshotRepository.UpdateAsync(snapshot, cancellationToken);
        return validation;
    }

    private bool ShouldTriggerFallback(OcrResultDto ocr, InvoiceValidationResult validation)
    {
        if (!_processingOptions.EnableFallback)
        {
            return false;
        }

        // Criterio de coste: sólo usamos el modelo caro cuando el OCR cae por debajo del umbral
        // o la validación determinista marca el documento como dudoso.
        if (ocr.Confidence < _processingOptions.OcrConfidenceThreshold)
        {
            return true;
        }

        return !validation.IsOk;
    }

    private string BuildFallbackReason(OcrResultDto ocr, InvoiceValidationResult validation)
    {
        var reasons = new List<string>();
        if (ocr.Confidence < _processingOptions.OcrConfidenceThreshold)
        {
            reasons.Add($"OCR {ocr.Confidence:P0}");
        }

        if (!validation.IsOk)
        {
            reasons.Add("Validación dudosa");
        }

        return string.Join(" | ", reasons);
    }

    private async Task<Guid> PersistInvoiceAsync(
        InvoiceExtractionResultDto extractedData,
        string filePath,
        decimal? extractionConfidence,
        InvoiceValidationResult validation,
        CancellationToken cancellationToken)
    {
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

        var issueDate = EnsureUtc(extractedData.GetIssueDate());
        var dueDate = extractedData.GetDueDate();
        var dueDateUtc = dueDate.HasValue ? EnsureUtc(dueDate.Value) : (DateTime?)null;

        var currency = string.IsNullOrWhiteSpace(extractedData.Currency) ? "EUR" : extractedData.Currency;
        var receivedDate = DateTime.UtcNow;

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
            filePath);

        if (!string.IsNullOrWhiteSpace(extractedData.SupplierTaxId))
        {
            invoice.SetSupplierTaxId(extractedData.SupplierTaxId);
        }

        if (extractedData.TaxAmount.HasValue)
        {
            invoice.SetTaxAmount(new Money(extractedData.TaxAmount.Value, currency));
            // Usar el tipo de IVA extraído si viene, o calcularlo
            if (extractedData.TaxRate.HasValue)
            {
                invoice.SetTaxRate(extractedData.TaxRate.Value);
            }
            else if (subtotalAmount > 0)
            {
                var taxRate = (extractedData.TaxAmount.Value / subtotalAmount) * 100m;
                invoice.SetTaxRate(taxRate);
            }
        }

        if (extractedData.SubtotalAmount.HasValue && extractedData.SubtotalAmount.Value != subtotalAmount)
        {
            invoice.SetSubtotalAmount(new Money(extractedData.SubtotalAmount.Value, currency));
        }

        // Agregar IRPF si viene en la factura (retención que se resta del total)
        if (extractedData.IrpfAmount.HasValue && extractedData.IrpfAmount.Value > 0)
        {
            invoice.SetIrpfAmount(new Money(extractedData.IrpfAmount.Value, currency));
            // Usar el tipo de IRPF extraído si viene, o calcularlo
            if (extractedData.IrpfRate.HasValue)
            {
                invoice.SetIrpfRate(extractedData.IrpfRate.Value);
            }
            else if (subtotalAmount > 0)
            {
                // Calcular tipo de IRPF si tenemos base e IRPF
                var irpfRate = (extractedData.IrpfAmount.Value / subtotalAmount) * 100m;
                invoice.SetIrpfRate(irpfRate);
            }
        }

        foreach (var lineDto in extractedData.Lines.OrderBy(l => l.LineNumber))
        {
            var line = new InvoiceReceivedLine(
                lineDto.LineNumber,
                lineDto.Description,
                lineDto.Quantity,
                new Money(lineDto.UnitPrice, currency),
                new Money(lineDto.LineTotal, currency));

            if (lineDto.TaxRate.HasValue)
            {
                line.SetTaxRate(lineDto.TaxRate);
            }

            invoice.AddLine(line);
        }

        invoice.SetExtractionConfidence(extractionConfidence);

        if (!validation.IsOk && validation.Errors.Any())
        {
            invoice.SetNotes("Validación determinista: " + string.Join("; ", validation.Errors));
        }

        _logger.LogDebug("Guardando factura en la base de datos...");
        await _repository.AddAsync(invoice, cancellationToken);

        return invoice.Id;
    }

    private static DateTime EnsureUtc(DateTime date)
    {
        return date.Kind == DateTimeKind.Utc
            ? date
            : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static string ComputeSha256(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

