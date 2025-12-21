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
    /// Modelo a utilizar (ej: gpt-4, gpt-3.5-turbo).
    /// </summary>
    public string Model { get; set; } = "gpt-4";
}

