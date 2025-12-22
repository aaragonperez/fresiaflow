using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Shared;
using Microsoft.Extensions.Options;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para subir y procesar facturas recibidas.
/// Orquesta la extracción completa de datos con IA, validación y persistencia.
/// Usa InvoiceReceived para mantener todos los datos fiscales y de detalle.
/// </summary>
public class UploadInvoiceUseCase : IUploadInvoiceUseCase
{
    private readonly IInvoiceReceivedRepository _invoiceReceivedRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IOpenAIClient _openAIClient;
    private readonly InvoiceExtractionPromptOptions _promptOptions;

    public UploadInvoiceUseCase(
        IInvoiceReceivedRepository invoiceReceivedRepository,
        IFileStorage fileStorage,
        IOpenAIClient openAIClient,
        IOptions<InvoiceExtractionPromptOptions> promptOptions)
    {
        _invoiceReceivedRepository = invoiceReceivedRepository;
        _fileStorage = fileStorage;
        _openAIClient = openAIClient;
        _promptOptions = promptOptions.Value;
    }

    public async Task<InvoiceResult> ExecuteAsync(UploadInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Guardar archivo
        var filePath = await _fileStorage.SaveFileAsync(
            command.FileStream,
            command.FileName,
            command.ContentType,
            cancellationToken);

        // 2. Usar IA para extraer datos estructurados directamente del PDF
        // OpenAI procesa el PDF directamente usando la API de archivos, lo que permite
        // mejor extracción de tablas, layout y formato visual
        // Usamos CompleteExtractionTemplate para obtener TODOS los campos fiscales y de detalle
        InvoiceExtractionResultDto extractedData;
        try
        {
            // Construir lista de empresas propias para el prompt
            var ownCompaniesList = _promptOptions.OwnCompanyNames != null && _promptOptions.OwnCompanyNames.Any()
                ? string.Join(", ", _promptOptions.OwnCompanyNames)
                : "NINGUNA";
            
            // Usar el template completo que incluye todos los campos fiscales y líneas de detalle
            var prompt = _promptOptions.CompleteExtractionTemplate
                .Replace("{0}", "Analiza el documento adjunto (PDF o imagen) y extrae todos los datos de la factura incluyendo información fiscal, importes desglosados y líneas de detalle.")
                .Replace("{1}", ownCompaniesList);
            
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,D", location = "UploadInvoiceUseCase.cs:56", message = "Prompt antes de llamar a OpenAI", data = new { promptLength = prompt?.Length ?? 0, promptPreview = prompt?.Length > 500 ? prompt.Substring(0, 500) + "..." : prompt, ownCompaniesList = ownCompaniesList }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
            // #endregion
            
            extractedData = await _openAIClient.ExtractStructuredDataFromPdfAsync<InvoiceExtractionResultDto>(
                filePath,
                prompt,
                cancellationToken);
            
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B,C,E", location = "UploadInvoiceUseCase.cs:64", message = "Datos extraídos por OpenAI", data = new { invoiceNumber = extractedData?.InvoiceNumber ?? "NULL", supplierName = extractedData?.SupplierName ?? "NULL", supplierNameLength = extractedData?.SupplierName?.Length ?? 0, supplierNameIsEmpty = string.IsNullOrWhiteSpace(extractedData?.SupplierName), totalAmount = extractedData?.TotalAmount ?? 0, hasSupplierTaxId = !string.IsNullOrWhiteSpace(extractedData?.SupplierTaxId), extractedDataJson = System.Text.Json.JsonSerializer.Serialize(extractedData) }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "UploadInvoiceUseCase.cs:67", message = "Excepción al extraer datos", data = new { exceptionType = ex.GetType().Name, exceptionMessage = ex.Message, innerException = ex.InnerException?.Message }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
            // #endregion
            throw new InvalidOperationException($"Error extrayendo datos de la factura: {ex.Message}", ex);
        }

        // 3. Validar datos mínimos requeridos
        if (string.IsNullOrWhiteSpace(extractedData.InvoiceNumber))
            throw new InvalidOperationException("El número de factura no puede estar vacío.");
        
        // #region agent log
        try { await System.IO.File.AppendAllTextAsync(@"c:\repo\FresiaFlow\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A,B", location = "UploadInvoiceUseCase.cs:74", message = "Validando supplierName antes de lanzar excepción", data = new { supplierName = extractedData?.SupplierName ?? "NULL", supplierNameLength = extractedData?.SupplierName?.Length ?? 0, supplierNameIsEmpty = string.IsNullOrWhiteSpace(extractedData?.SupplierName), supplierNameIsNull = extractedData?.SupplierName == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n", cancellationToken); } catch { }
        // #endregion
        
        // Si no se pudo obtener el proveedor, usar "<desconocido>" para permitir edición posterior
        if (string.IsNullOrWhiteSpace(extractedData.SupplierName))
        {
            extractedData.SupplierName = "<desconocido>";
        }

        if (extractedData.TotalAmount <= 0)
            throw new InvalidOperationException("El total de la factura debe ser mayor que cero.");

        // 4. Verificar duplicados
        var existing = await _invoiceReceivedRepository.GetByInvoiceNumberAsync(extractedData.InvoiceNumber, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Ya existe una factura con el número {extractedData.InvoiceNumber}");
        }

        // 5. Convertir fechas a UTC para PostgreSQL
        var issueDate = EnsureUtc(extractedData.GetIssueDate());
        var receivedDate = DateTime.UtcNow; // Fecha de recepción = ahora (contable)
        var dueDate = extractedData.GetDueDate();

        // 6. Crear entidad de dominio InvoiceReceived con todos los datos
        var currency = string.IsNullOrWhiteSpace(extractedData.Currency) ? "EUR" : extractedData.Currency;
        
        // Asegurar que los valores del OCR no sean negativos
        var totalAmount = Math.Max(0, extractedData.TotalAmount);
        var taxAmount = extractedData.TaxAmount.HasValue ? Math.Max(0, extractedData.TaxAmount.Value) : 0m;
        
        // Calcular base imponible: si no viene, calcularla restando IVA del total
        var subtotalAmount = extractedData.SubtotalAmount.HasValue 
            ? Math.Max(0, extractedData.SubtotalAmount.Value)
            : Math.Max(0, totalAmount - taxAmount);
        
        var invoice = new InvoiceReceived(
            extractedData.InvoiceNumber,
            extractedData.SupplierName,
            issueDate,
            receivedDate,
            new Money(subtotalAmount, currency),
            new Money(totalAmount, currency),
            currency,
            InvoiceOrigin.ManualUpload,
            filePath);

        // Configurar campos opcionales
        if (!string.IsNullOrWhiteSpace(extractedData.SupplierTaxId))
            invoice.SetSupplierTaxId(extractedData.SupplierTaxId);

        if (extractedData.TaxAmount.HasValue && taxAmount > 0)
        {
            invoice.SetTaxAmount(new Money(taxAmount, currency));
            // Calcular tipo de IVA si tenemos base e IVA
            if (subtotalAmount > 0)
            {
                var taxRate = (taxAmount / subtotalAmount) * 100m;
                invoice.SetTaxRate(taxRate);
            }
        }

        // La base imponible ya se estableció en el constructor, pero si viene explícitamente la actualizamos
        if (extractedData.SubtotalAmount.HasValue)
        {
            var explicitSubtotal = Math.Max(0, extractedData.SubtotalAmount.Value);
            if (explicitSubtotal != subtotalAmount)
            {
                invoice.SetSubtotalAmount(new Money(explicitSubtotal, currency));
            }
        }

        // Agregar líneas de detalle
        foreach (var lineDto in extractedData.Lines.OrderBy(l => l.LineNumber))
        {
            // Asegurar que los valores no sean negativos (pueden venir mal del OCR)
            var unitPrice = Math.Max(0, lineDto.UnitPrice);
            var lineTotal = Math.Max(0, lineDto.LineTotal);
            
            var line = new InvoiceReceivedLine(
                lineDto.LineNumber,
                lineDto.Description,
                lineDto.Quantity,
                new Money(unitPrice, currency),
                new Money(lineTotal, currency));

            if (lineDto.TaxRate.HasValue)
                line.SetTaxRate(lineDto.TaxRate);

            invoice.AddLine(line);
        }

        // 7. Calcular confidence basado en completitud de datos
        var confidence = CalculateConfidence(extractedData);
        invoice.SetExtractionConfidence(confidence);

        // 8. Persistir
        await _invoiceReceivedRepository.AddAsync(invoice, cancellationToken);

        return new InvoiceResult(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.TotalAmount.Value,
            invoice.SupplierName,
            RequiresReview: false); // Ya no hay estados, todas las facturas están contabilizadas
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

    /// <summary>
    /// Calcula el nivel de confianza basado en la completitud de los datos extraídos.
    /// </summary>
    private static decimal CalculateConfidence(InvoiceExtractionResultDto data)
    {
        decimal score = 1.0m;
        
        // Penalizar campos faltantes
        if (string.IsNullOrWhiteSpace(data.SupplierTaxId))
            score -= 0.1m;
        
        if (!data.TaxAmount.HasValue)
            score -= 0.1m;
        
        if (!data.SubtotalAmount.HasValue)
            score -= 0.1m;
        
        if (data.Lines == null || data.Lines.Count == 0)
            score -= 0.2m;
        
        return Math.Max(0.0m, Math.Min(1.0m, score));
    }
}

