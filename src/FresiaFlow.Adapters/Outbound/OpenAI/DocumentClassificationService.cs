using System.Linq;
using System.Text.Json;
using System.Text;
using FresiaFlow.Application.InvoicesReceived;
using FresiaFlow.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FresiaFlow.Adapters.Outbound.OpenAI;

/// <summary>
/// Clasifica documentos con un modelo ligero para no disparar costes innecesarios.
/// </summary>
public class DocumentClassificationService : IDocumentClassificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly DocumentClassificationOptions _classificationOptions;
    private readonly ILogger<DocumentClassificationService> _logger;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DocumentClassificationService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<DocumentClassificationOptions> classificationOptions,
        ILogger<DocumentClassificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _classificationOptions = classificationOptions.Value;
        _logger = logger;
    }

    public async Task<DocumentClassificationResultDto> ClassifyAsync(
        OcrResultDto ocr,
        CancellationToken cancellationToken = default)
    {
        if (ocr == null || string.IsNullOrWhiteSpace(ocr.Text))
        {
            return new DocumentClassificationResultDto();
        }

        // Limitar el prompt para que el modelo barato siga siendo económico.
        var normalizedText = Truncate(ocr.Text, 4000);
        var prompt = BuildPrompt(normalizedText);

        var content = await CallOpenAiAsync(prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new DocumentClassificationResultDto();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ClassificationPayload>(content, SerializerOptions);
            if (payload == null)
            {
                return new DocumentClassificationResultDto { RawJson = content };
            }

            return new DocumentClassificationResultDto
            {
                DocumentType = payload.DocumentType ?? "unknown",
                Language = payload.Language ?? "es",
                SupplierGuess = payload.SupplierGuess,
                ProviderId = payload.ProviderId,
                Confidence = payload.Confidence,
                RawJson = content
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No se pudo parsear la clasificación. Se devolverá valor por defecto.");
            return new DocumentClassificationResultDto { RawJson = content };
        }
    }

    private string BuildPrompt(string ocrText)
    {
        return $@"Clasifica el siguiente documento. Devuelve SOLO JSON válido con esta forma:
{{
  ""documentType"": ""invoice|ticket|credit_note|other"",
  ""language"": ""es|en|fr|... (ISO-639-1)"",
  ""supplierGuess"": ""nombre probable del proveedor"",
  ""providerId"": ""id opcional"",
  ""confidence"": decimal entre 0 y 1
}}

Texto:
{ocrText}";
    }

    private async Task<string?> CallOpenAiAsync(string prompt, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("OpenAI");
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiOptions.ApiKey}");

        var requestBody = new
        {
            model = string.IsNullOrWhiteSpace(_classificationOptions.Model)
                ? _openAiOptions.Model
                : _classificationOptions.Model,
            messages = new[]
            {
                new { role = "system", content = _classificationOptions.SystemPrompt },
                new { role = "user", content = prompt }
            },
            max_tokens = _classificationOptions.MaxTokens,
            temperature = 0
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error clasificando documento: {Status} - {Error}", response.StatusCode, error);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson, SerializerOptions);
        var rawContent = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return null;
        }

        if (rawContent.StartsWith("```"))
        {
            rawContent = rawContent.Replace("```json", string.Empty)
                                   .Replace("```", string.Empty)
                                   .Trim();
        }

        return rawContent;
    }

    private static string Truncate(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
        {
            return input;
        }

        return input[..maxLength];
    }

    private sealed class ClassificationPayload
    {
        public string? DocumentType { get; set; }
        public string? Language { get; set; }
        public string? SupplierGuess { get; set; }
        public string? ProviderId { get; set; }
        public decimal Confidence { get; set; }
    }

    private sealed class OpenAiChatResponse
    {
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
    }

    private sealed class Choice
    {
        public Message Message { get; set; } = new();
    }

    private sealed class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}

