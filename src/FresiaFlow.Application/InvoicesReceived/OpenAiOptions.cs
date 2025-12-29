namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Opciones de configuración para OpenAI API.
/// </summary>
public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// API Key de OpenAI.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Modelo económico por defecto.
    /// </summary>
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Modelo de alta precisión reservado para el fallback (<15% de documentos).
    /// </summary>
    public string? FallbackModel { get; set; } = "gpt-4o";
}

