namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Configuración del clasificador ligero.
/// </summary>
public class DocumentClassificationOptions
{
    public const string SectionName = "InvoiceProcessing:Classification";

    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 300;
    public string SystemPrompt { get; set; } =
        "Eres un clasificador contable. Respondes SOLO con JSON estricto.";
}

