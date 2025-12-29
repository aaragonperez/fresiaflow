using FresiaFlow.Application.AI;
using FresiaFlow.Application.Ports.Outbound;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador para el chat con router FresiaFlow.
/// Todas las interacciones pasan por el router que selecciona el agente adecuado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IFresiaFlowRouter _router;
    private readonly IChatAIClient _openAIClient;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IFresiaFlowRouter router,
        IChatAIClient openAIClient,
        ILogger<ChatController> logger)
    {
        _router = router;
        _openAIClient = openAIClient;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un mensaje del usuario usando el router FresiaFlow.
    /// El router selecciona el agente adecuado y mantiene el histórico conversacional.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "El mensaje no puede estar vacío" });
            }

            // Convertir DTOs a ChatMessageHistory
            var history = (request.History ?? new List<ChatMessageDto>())
                .Select(h => new ChatMessageHistory
                {
                    Role = h.Role,
                    Content = h.Content
                })
                .ToList();

            // Procesar con contexto de pantalla (sin usar agentes de programación)
            var response = await ProcessMessageWithScreenContextAsync(
                request.Message,
                history,
                request.ScreenContext,
                cancellationToken);

            return Ok(new
            {
                content = response.Content,
                agent = response.SelectedAgent,
                actions = response.Actions.Select(a => new
                {
                    type = a.Type,
                    @params = a.Params
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje del chat");
            return StatusCode(500, new { error = "Error al procesar el mensaje. Por favor, inténtalo de nuevo." });
        }
    }

    /// <summary>
    /// Procesa un mensaje con contexto de pantalla, sin usar agentes de programación.
    /// </summary>
    private async Task<RouterResponse> ProcessMessageWithScreenContextAsync(
        string userMessage,
        List<ChatMessageHistory> history,
        ScreenContextDto? screenContext,
        CancellationToken cancellationToken)
    {
        // Construir system prompt basado en el contexto de la pantalla
        var systemPrompt = BuildScreenContextPrompt(screenContext);

        // Construir mensajes con histórico
        var messages = new List<ChatMessageHistory>();
        messages.Add(new ChatMessageHistory { Role = "system", Content = systemPrompt });
        messages.AddRange(history);
        messages.Add(new ChatMessageHistory { Role = "user", Content = userMessage });

        // Llamar a OpenAI directamente sin router de agentes
        var response = await _openAIClient.GetChatCompletionAsync(
            systemPrompt,
            userMessage,
            cancellationToken);

        // Extraer acciones si el modelo las sugiere
        var actions = ExtractActionsFromResponse(response);

        return new RouterResponse
        {
            Content = response,
            SelectedAgent = "SCREEN_ASSISTANT",
            Actions = actions
        };
    }

    /// <summary>
    /// Construye el system prompt basado en el contexto de la pantalla.
    /// </summary>
    private string BuildScreenContextPrompt(ScreenContextDto? screenContext)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Eres un asistente de IA que ayuda al usuario a interactuar con la aplicación FresiaFlow.");
        prompt.AppendLine("Tu función es responder preguntas sobre los datos visibles en la pantalla actual y ayudar a ejecutar acciones.");
        prompt.AppendLine();
        prompt.AppendLine("IMPORTANTE: NO eres un agente de programación. NO debes generar código, explicar arquitectura, o ayudar con desarrollo.");
        prompt.AppendLine("Tu único objetivo es ayudar al usuario a usar la aplicación y entender los datos que ve en pantalla.");
        prompt.AppendLine();

        if (screenContext != null)
        {
            prompt.AppendLine($"Pantalla actual: {screenContext.PageTitle}");
            prompt.AppendLine($"Ruta: {screenContext.Route}");
            prompt.AppendLine();

            if (screenContext.VisibleData != null)
            {
                prompt.AppendLine("Datos visibles en la pantalla:");
                prompt.AppendLine(System.Text.Json.JsonSerializer.Serialize(screenContext.VisibleData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                prompt.AppendLine();
            }

            if (screenContext.AvailableActions != null && screenContext.AvailableActions.Count > 0)
            {
                prompt.AppendLine("Acciones disponibles:");
                foreach (var action in screenContext.AvailableActions)
                {
                    prompt.AppendLine($"- {action}");
                }
                prompt.AppendLine();
            }

            if (screenContext.ComponentState != null)
            {
                prompt.AppendLine("Estado de componentes:");
                prompt.AppendLine(System.Text.Json.JsonSerializer.Serialize(screenContext.ComponentState, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                prompt.AppendLine();
            }
        }

        prompt.AppendLine("Responde de forma natural y útil. Si el usuario pregunta sobre datos, usa la información del contexto de pantalla.");
        prompt.AppendLine("Si el usuario quiere ejecutar una acción, puedes sugerirla usando el formato de acción JSON.");

        return prompt.ToString();
    }

    /// <summary>
    /// Extrae acciones de la respuesta del modelo si están en formato JSON.
    /// </summary>
    private List<ChatAction> ExtractActionsFromResponse(string response)
    {
        var actions = new List<ChatAction>();
        
        // Buscar bloques JSON en la respuesta
        var jsonPattern = @"\{[\s\S]*?""type""[\s\S]*?\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(response, jsonPattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            try
            {
                var action = System.Text.Json.JsonSerializer.Deserialize<ChatAction>(match.Value);
                if (action != null && !string.IsNullOrEmpty(action.Type))
                {
                    actions.Add(action);
                }
            }
            catch
            {
                // Ignorar JSONs inválidos
            }
        }

        return actions;
    }
}

/// <summary>
/// Request para el chat.
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto>? History { get; set; }
    public ScreenContextDto? ScreenContext { get; set; }
}

/// <summary>
/// Contexto de la pantalla actual.
/// </summary>
public class ScreenContextDto
{
    public string Route { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public object? VisibleData { get; set; }
    public List<string>? AvailableActions { get; set; }
    public Dictionary<string, object>? ComponentState { get; set; }
}

/// <summary>
/// DTO para mensajes del historial.
/// </summary>
public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" o "assistant"
    public string Content { get; set; } = string.Empty;
}

