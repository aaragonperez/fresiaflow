using System.Text;
using System.Text.Json;
using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FresiaFlow.Adapters.Outbound.OpenAI;

/// <summary>
/// Servicio de extracción semántica de facturas usando OpenAI API.
/// </summary>
public class InvoiceExtractionService : IInvoiceExtractionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _options;
    private readonly InvoiceExtractionPromptOptions _promptOptions;
    private readonly ILogger<InvoiceExtractionService> _logger;

    public InvoiceExtractionService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        IOptions<InvoiceExtractionPromptOptions> promptOptions,
        ILogger<InvoiceExtractionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _promptOptions = promptOptions.Value;
        _logger = logger;
    }

    public async Task<InvoiceExtractionResultDto> ExtractInvoiceDataAsync(
        InvoiceExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Iniciando extracción estructurada (highPrecision={HighPrecision})", request.UseHighPrecisionModel);

        var prompt = BuildExtractionPrompt(request.OcrText);
        var model = ResolveModel(request.UseHighPrecisionModel);
        var response = await CallOpenAiAsync(prompt, model, cancellationToken);

        _logger.LogDebug("Respuesta de OpenAI recibida, parseando JSON");

        try
        {
            var result = JsonSerializer.Deserialize<InvoiceExtractionResultDto>(
                response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
            {
                throw new InvalidOperationException("OpenAI devolvió una respuesta vacía");
            }

            // VALIDACIÓN DE SEGURIDAD: Asegurar que FRESIA nunca sea el proveedor
            result = ValidateAndCorrectSupplier(result);

            // Normalizar y validar el CIF/NIF del proveedor
            result.SupplierTaxId = TaxIdNormalizer.Normalize(result.SupplierTaxId);

            if (!string.IsNullOrWhiteSpace(result.SupplierTaxId))
            {
                _logger.LogInformation(
                    "CIF/NIF del proveedor extraído y normalizado: {SupplierTaxId}",
                    result.SupplierTaxId);
            }
            else if (string.IsNullOrWhiteSpace(result.SupplierTaxId) && !string.IsNullOrWhiteSpace(result.SupplierName))
            {
                _logger.LogWarning(
                    "⚠️ No se pudo extraer el CIF/NIF del proveedor '{SupplierName}'. " +
                    "Se recomienda revisar manualmente la factura.",
                    result.SupplierName);
            }

            _logger.LogInformation(
                "Factura extraída: {InvoiceNumber} - {SupplierName} - {TotalAmount} {Currency} - CIF: {SupplierTaxId}",
                result.InvoiceNumber,
                result.SupplierName,
                result.TotalAmount,
                result.Currency,
                result.SupplierTaxId ?? "NO DISPONIBLE");

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parseando respuesta JSON de OpenAI: {Response}", response);
            throw new InvalidOperationException("No se pudo parsear la respuesta de OpenAI", ex);
        }
    }

    private string BuildExtractionPrompt(string invoiceText)
    {
        // Construir lista de empresas propias para el prompt
        var ownCompaniesList = _promptOptions.OwnCompanyNames != null && _promptOptions.OwnCompanyNames.Any()
            ? string.Join(", ", _promptOptions.OwnCompanyNames)
            : "NINGUNA";
        
        // Usar prompt desde configuración con empresas propias
        return _promptOptions.CompleteExtractionTemplate
            .Replace("{0}", invoiceText)
            .Replace("{1}", ownCompaniesList);
    }

    /// <summary>
    /// Valida y corrige el resultado de extracción para asegurar que FRESIA nunca sea el proveedor.
    /// Esta es una capa de seguridad adicional por si el LLM comete un error.
    /// </summary>
    private InvoiceExtractionResultDto ValidateAndCorrectSupplier(InvoiceExtractionResultDto result)
    {
        if (string.IsNullOrWhiteSpace(result.SupplierName))
        {
            return result;
        }

        // Verificar si el supplierName contiene alguna empresa propia
        var ownCompanies = _promptOptions.OwnCompanyNames ?? new List<string>();
        var supplierUpper = result.SupplierName.ToUpperInvariant();
        
        bool isOwnCompany = ownCompanies.Any(own => 
            supplierUpper.Contains(own.ToUpperInvariant()) ||
            own.ToUpperInvariant().Contains(supplierUpper));
        
        // También verificar explícitamente "FRESIA" por si no está en la lista
        if (!isOwnCompany && supplierUpper.Contains("FRESIA"))
        {
            isOwnCompany = true;
        }

        if (isOwnCompany)
        {
            _logger.LogWarning(
                "⚠️ CORRECCIÓN: Se detectó empresa propia '{SupplierName}' como proveedor. " +
                "Esto indica un error de extracción. Marcando como 'PROVEEDOR NO IDENTIFICADO'.",
                result.SupplierName);
            
            // Marcar como no identificado en lugar de usar Fresia
            result.SupplierName = "PROVEEDOR NO IDENTIFICADO - REVISAR MANUALMENTE";
            
            // También limpiar el TaxId si es el de Fresia (B87392700)
            if (TaxIdNormalizer.IsFresiaTaxId(result.SupplierTaxId))
            {
                _logger.LogWarning("⚠️ CORRECCIÓN: Se detectó CIF de Fresia (B87392700) como proveedor. Limpiando.");
                result.SupplierTaxId = null;
            }
        }

        return result;
    }

    private string ResolveModel(bool useHighPrecision)
    {
        if (useHighPrecision && !string.IsNullOrWhiteSpace(_options.FallbackModel))
        {
            return _options.FallbackModel!;
        }

        return _options.Model;
    }

    private async Task<string> CallOpenAiAsync(string prompt, string model, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("OpenAI");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = _promptOptions.SystemMessage },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 2000
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogDebug("Llamando a OpenAI API con modelo {Model}", model);

        var response = await httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Error en llamada a OpenAI: {StatusCode} - {Error}",
                response.StatusCode,
                errorContent);
            throw new InvalidOperationException($"Error llamando a OpenAI: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson);

        if (openAiResponse?.Choices == null || openAiResponse.Choices.Length == 0)
        {
            throw new InvalidOperationException("OpenAI no devolvió ninguna respuesta válida");
        }

        var extractedJson = openAiResponse.Choices[0].Message.Content.Trim();

        // Limpiar markdown si existe
        if (extractedJson.StartsWith("```json"))
        {
            extractedJson = extractedJson.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (extractedJson.StartsWith("```"))
        {
            extractedJson = extractedJson.Replace("```", "").Trim();
        }

        return extractedJson;
    }

    // DTOs internos para deserializar respuesta de OpenAI
    private class OpenAiChatResponse
    {
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
    }

    private class Choice
    {
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}

