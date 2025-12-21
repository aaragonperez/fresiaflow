using FresiaFlow.Application.Ports.Outbound;
using System.Text;
using System.Text.Json;

namespace FresiaFlow.Adapters.Outbound.OpenAI;

/// <summary>
/// Adapter para integración con OpenAI API.
/// Aísla los detalles de implementación de OpenAI del dominio.
/// </summary>
public class OpenAIAdapter : IOpenAIClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAIAdapter(HttpClient httpClient, string apiKey, string model = "gpt-4")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<string> GetChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implementar llamada real a OpenAI API
        // Por ahora retorna un stub
        await Task.CompletedTask;
        return $"STUB: Respuesta de OpenAI para: {userMessage}";
    }

    public async Task<ToolCallResult> GetChatCompletionWithToolsAsync(
        string systemPrompt,
        string userMessage,
        List<ToolDefinition> availableTools,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implementar llamada con tool calling
        // Formato de request:
        // {
        //   "model": "gpt-4",
        //   "messages": [...],
        //   "tools": [...]
        // }
        
        await Task.CompletedTask;
        return new ToolCallResult(
            "STUB: Respuesta con tools",
            new List<ToolCall>());
    }

    public async Task<T> ExtractStructuredDataAsync<T>(
        string text,
        string schemaDescription,
        CancellationToken cancellationToken = default) where T : class
    {
        var prompt = $@"{schemaDescription}

Texto a analizar:
{text}

IMPORTANTE: Responde ÚNICAMENTE con un objeto JSON válido. No incluyas markdown, comentarios ni texto adicional. Solo el JSON.";

        // Solo usar response_format con modelos que lo soporten (gpt-4-turbo, gpt-3.5-turbo)
        // gpt-4 base no soporta response_format, causa error 400
        var supportsJsonMode = _model.Contains("turbo") || _model.Contains("gpt-4o");
        
        object requestBody;
        
        if (supportsJsonMode)
        {
            requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "Eres un asistente experto en extracción de datos estructurados. Respondes ÚNICAMENTE con JSON válido, sin markdown ni texto adicional." },
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1,
                max_tokens = 2000
            };
        }
        else
        {
            requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "Eres un asistente experto en extracción de datos estructurados. Respondes ÚNICAMENTE con JSON válido, sin markdown ni texto adicional." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens = 2000
            };
        }

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"OpenAI API error ({response.StatusCode}): {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(responseJson))
            throw new InvalidOperationException("OpenAI devolvió una respuesta vacía");
        
        var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (openAiResponse?.Choices == null || (openAiResponse?.Choices?.Length ?? 0) == 0)
        {
            throw new InvalidOperationException("OpenAI no devolvió ninguna respuesta válida");
        }

        var extractedJson = openAiResponse?.Choices?[0]?.Message?.Content?.Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(extractedJson))
        {
            throw new InvalidOperationException("OpenAI devolvió una respuesta vacía");
        }
        
        // Limpiar markdown si existe
        if (extractedJson.StartsWith("```json"))
        {
            extractedJson = extractedJson.Substring(7);
        }
        if (extractedJson.StartsWith("```"))
        {
            extractedJson = extractedJson.Substring(3);
        }
        if (extractedJson.EndsWith("```"))
        {
            extractedJson = extractedJson.Substring(0, extractedJson.Length - 3);
        }
        extractedJson = extractedJson.Trim();

        T? result;
        try
        {
            result = JsonSerializer.Deserialize<T>(
                extractedJson ?? string.Empty,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Error deserializando JSON de OpenAI: {ex.Message}", ex);
        }

        if (result == null)
        {
            throw new InvalidOperationException("No se pudo deserializar la respuesta de OpenAI");
        }

        return result;
    }

    private class OpenAiChatResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public OpenAiChoice[]? Choices { get; set; }
    }

    private class OpenAiChoice
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private class OpenAiMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private async Task<string> CallOpenAIAsync(object requestBody, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        // TODO: Parsear respuesta y extraer mensaje/tool calls

        return responseJson;
    }
}

