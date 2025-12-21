namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para integración con OpenAI API.
/// Aísla el dominio de los detalles de implementación de OpenAI.
/// </summary>
public interface IOpenAIClient
{
    /// <summary>
    /// Envía un mensaje al chat y obtiene una respuesta.
    /// </summary>
    Task<string> GetChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía un mensaje con tool calling habilitado.
    /// </summary>
    Task<ToolCallResult> GetChatCompletionWithToolsAsync(
        string systemPrompt,
        string userMessage,
        List<ToolDefinition> availableTools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extrae información estructurada de un texto usando IA.
    /// </summary>
    Task<T> ExtractStructuredDataAsync<T>(
        string text,
        string schemaDescription,
        CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Definición de una herramienta (tool) para OpenAI.
/// </summary>
public record ToolDefinition(
    string Name,
    string Description,
    object Parameters
);

/// <summary>
/// Resultado de una llamada con tool calling.
/// </summary>
public record ToolCallResult(
    string? Message,
    List<ToolCall> ToolCalls
);

/// <summary>
/// Llamada a una herramienta solicitada por OpenAI.
/// </summary>
public record ToolCall(
    string ToolName,
    string ArgumentsJson
);

