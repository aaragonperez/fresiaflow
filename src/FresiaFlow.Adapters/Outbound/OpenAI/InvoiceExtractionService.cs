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
        string text, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Iniciando extracción semántica de factura con OpenAI");

        var prompt = BuildExtractionPrompt(text);
        var response = await CallOpenAiAsync(prompt, cancellationToken);

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

            _logger.LogInformation(
                "Factura extraída: {InvoiceNumber} - {SupplierName} - {TotalAmount} {Currency}",
                result.InvoiceNumber,
                result.SupplierName,
                result.TotalAmount,
                result.Currency);

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

    private async Task<string> CallOpenAiAsync(string prompt, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("OpenAI");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");

        var requestBody = new
        {
            model = _options.Model,
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

        _logger.LogDebug("Llamando a OpenAI API con modelo {Model}", _options.Model);

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

