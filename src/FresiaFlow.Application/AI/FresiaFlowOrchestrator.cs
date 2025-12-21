using FresiaFlow.Application.Ports.Outbound;

namespace FresiaFlow.Application.AI;

/// <summary>
/// Orquestador principal de IA para FresiaFlow.
/// Coordina las interacciones con OpenAI API y el uso de herramientas.
/// </summary>
public interface IFresiaFlowOrchestrator
{
    /// <summary>
    /// Genera un plan diario basado en contexto y procedimientos.
    /// </summary>
    Task<DailyPlanResponse> GenerateDailyPlanAsync(
        DateTime date,
        object context,
        List<string> relevantProcedures,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa una consulta del usuario usando RAG y tool calling.
    /// </summary>
    Task<string> ProcessUserQueryAsync(
        string userQuery,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación del orquestador de IA.
/// </summary>
public class FresiaFlowOrchestrator : IFresiaFlowOrchestrator
{
    private readonly IOpenAIClient _openAIClient;
    private readonly IVectorStore _vectorStore;
    private readonly ToolRegistry _toolRegistry;

    public FresiaFlowOrchestrator(
        IOpenAIClient openAIClient,
        IVectorStore vectorStore,
        ToolRegistry toolRegistry)
    {
        _openAIClient = openAIClient;
        _vectorStore = vectorStore;
        _toolRegistry = toolRegistry;
    }

    public async Task<DailyPlanResponse> GenerateDailyPlanAsync(
        DateTime date,
        object context,
        List<string> relevantProcedures,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(relevantProcedures);
        var userMessage = BuildUserMessage(date, context);

        var tools = _toolRegistry.GetAvailableTools();
        var result = await _openAIClient.GetChatCompletionWithToolsAsync(
            systemPrompt,
            userMessage,
            tools,
            cancellationToken);

        // Procesar tool calls si existen
        if (result.ToolCalls.Count > 0)
        {
            // Ejecutar herramientas y obtener respuesta final
            var toolResults = await ExecuteToolsAsync(result.ToolCalls, cancellationToken);
            var finalResponse = await _openAIClient.GetChatCompletionAsync(
                systemPrompt,
                $"Resultados de herramientas: {string.Join(", ", toolResults)}",
                cancellationToken);

            return ParseDailyPlanResponse(finalResponse);
        }

        return ParseDailyPlanResponse(result.Message ?? "");
    }

    public async Task<string> ProcessUserQueryAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        // 1. Buscar contexto relevante en RAG
        var searchResults = await _vectorStore.SearchSimilarAsync(userQuery, topK: 5, cancellationToken);

        // 2. Construir prompt con contexto
        var context = string.Join("\n", searchResults.Select(r => r.Content));
        var systemPrompt = $"Eres Fresia, la secretaria administrativa virtual. Usa el siguiente contexto:\n{context}";
        
        // 3. Obtener herramientas disponibles
        var tools = _toolRegistry.GetAvailableTools();

        // 4. Procesar con tool calling
        var result = await _openAIClient.GetChatCompletionWithToolsAsync(
            systemPrompt,
            userQuery,
            tools,
            cancellationToken);

        // 5. Ejecutar herramientas si es necesario
        if (result.ToolCalls.Count > 0)
        {
            var toolResults = await ExecuteToolsAsync(result.ToolCalls, cancellationToken);
            return await _openAIClient.GetChatCompletionAsync(
                systemPrompt,
                $"Consulta: {userQuery}\nResultados: {string.Join(", ", toolResults)}",
                cancellationToken);
        }

        return result.Message ?? "No pude procesar tu consulta.";
    }

    private string BuildSystemPrompt(List<string> relevantProcedures)
    {
        var proceduresText = string.Join("\n", relevantProcedures);
        return $@"Eres Fresia, la secretaria administrativa virtual de una micro-pyme.
Tu función es ayudar con gestión administrativa y financiera.

Procedimientos internos relevantes:
{proceduresText}

Genera planes diarios claros, priorizados y accionables.";
    }

    private string BuildUserMessage(DateTime date, object context)
    {
        return $"Genera un plan diario para {date:yyyy-MM-dd}.\nContexto: {System.Text.Json.JsonSerializer.Serialize(context)}";
    }

    private async Task<List<string>> ExecuteToolsAsync(List<ToolCall> toolCalls, CancellationToken cancellationToken)
    {
        var results = new List<string>();

        foreach (var toolCall in toolCalls)
        {
            var result = await _toolRegistry.ExecuteToolAsync(toolCall.ToolName, toolCall.ArgumentsJson, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private DailyPlanResponse ParseDailyPlanResponse(string response)
    {
        // TODO: Parsear respuesta JSON estructurada de OpenAI
        return new DailyPlanResponse
        {
            Summary = response,
            ProposedTasks = new List<ProposedTask>(),
            Recommendations = new List<string>()
        };
    }
}

/// <summary>
/// Respuesta del plan diario generado por IA.
/// </summary>
public class DailyPlanResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<ProposedTask> ProposedTasks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Tarea propuesta por la IA.
/// </summary>
public class ProposedTask
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
}

