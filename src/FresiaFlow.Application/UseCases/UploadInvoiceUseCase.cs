using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Shared;
using Microsoft.Extensions.Options;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para subir y procesar facturas.
/// Orquesta la extracción de datos, validación y persistencia.
/// </summary>
public class UploadInvoiceUseCase : IUploadInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IOpenAIClient _openAIClient;
    private readonly IPdfTextExtractorService _pdfTextExtractor;
    private readonly InvoiceExtractionPromptOptions _promptOptions;

    public UploadInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IFileStorage fileStorage,
        IOpenAIClient openAIClient,
        IPdfTextExtractorService pdfTextExtractor,
        IOptions<InvoiceExtractionPromptOptions> promptOptions)
    {
        _invoiceRepository = invoiceRepository;
        _fileStorage = fileStorage;
        _openAIClient = openAIClient;
        _pdfTextExtractor = pdfTextExtractor;
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

        // 2. Extraer texto del PDF
        var pdfText = await _pdfTextExtractor.ExtractTextAsync(filePath, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(pdfText))
        {
            throw new InvalidOperationException("No se pudo extraer texto del PDF.");
        }

        // 3. Usar IA para extraer datos estructurados
        InvoiceExtractionResult invoiceData;
        try
        {
            // Usar prompt desde configuración
            var prompt = string.Format(_promptOptions.BasicExtractionTemplate, pdfText);
            
            invoiceData = await _openAIClient.ExtractStructuredDataAsync<InvoiceExtractionResult>(
                pdfText,
                prompt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error extrayendo datos de la factura: {ex.Message}", ex);
        }

        // 4. Validar y crear entidad
        
        var invoiceNumber = invoiceData.GetInvoiceNumber();
        var supplierName = invoiceData.GetSupplierName();
        
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new InvalidOperationException("El número de factura no puede estar vacío.");
        
        if (string.IsNullOrWhiteSpace(supplierName))
            throw new InvalidOperationException("El nombre del proveedor no puede estar vacío.");
        
        var invoice = new Invoice(
            invoiceNumber,
            invoiceData.GetIssueDate(),
            invoiceData.GetDueDate(),
            new Money(invoiceData.GetAmount()),
            supplierName,
            filePath);

        // 5. Verificar duplicados
        var existing = await _invoiceRepository.GetByInvoiceNumberAsync(invoice.InvoiceNumber, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Ya existe una factura con el número {invoice.InvoiceNumber}");
        }

        // 6. Persistir
        await _invoiceRepository.AddAsync(invoice, cancellationToken);

        return new InvoiceResult(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Amount.Value,
            invoice.SupplierName,
            RequiresReview: invoiceData.GetConfidence() < 0.8m);
    }

    private class InvoiceExtractionResult
    {
        // Propiedades normales para deserialización estándar
        [System.Text.Json.Serialization.JsonPropertyName("InvoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("IssueDate")]
        public string? IssueDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("DueDate")]
        public string? DueDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Amount")]
        public System.Text.Json.JsonElement? Amount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("SupplierName")]
        public string? SupplierName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Confidence")]
        public System.Text.Json.JsonElement? Confidence { get; set; }

        // Propiedades calculadas que parsean los valores
        public string GetInvoiceNumber()
        {
            return InvoiceNumber ?? string.Empty;
        }

        public DateTime GetIssueDate()
        {
            return ParseDate(IssueDate ?? string.Empty);
        }

        public DateTime? GetDueDate()
        {
            if (string.IsNullOrWhiteSpace(DueDate)) return null;
            return ParseDate(DueDate);
        }

        public decimal GetAmount()
        {
            if (!Amount.HasValue) return 0m;
            return ParseAmount(Amount.Value);
        }

        public string GetSupplierName()
        {
            return SupplierName ?? string.Empty;
        }

        public decimal GetConfidence()
        {
            if (!Confidence.HasValue) return 0.5m;
            var element = Confidence.Value;
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
                return element.GetDecimal();
            if (element.ValueKind == System.Text.Json.JsonValueKind.String && decimal.TryParse(element.GetString(), out var dec))
                return dec;
            return 0.5m;
        }

        private static DateTime ParseDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return DateTime.UtcNow;

            // Intentar formato ISO (YYYY-MM-DD)
            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var isoDate))
                return isoDate;

            // Intentar formato español (DD/MM/YYYY)
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var esDate))
                return esDate;

            // Intentar parseo genérico
            if (DateTime.TryParse(dateStr, out var parsedDate))
                return parsedDate;

            return DateTime.UtcNow;
        }

        private static decimal ParseAmount(System.Text.Json.JsonElement element)
        {
            // Si es número, devolverlo directamente
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return element.GetDecimal();
            }

            // Si es string, parsearlo
            if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var str = element.GetString() ?? "0";
                
                // Remover símbolos de moneda y espacios
                str = str.Replace("€", "").Replace("$", "").Replace("£", "").Replace("EUR", "").Trim();
                
                // Reemplazar coma por punto para decimales
                str = str.Replace(",", ".");

                if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                    return result;
            }

            return 0m;
        }
    }
}

