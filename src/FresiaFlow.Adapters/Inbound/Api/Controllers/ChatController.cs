using FresiaFlow.Application.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IFresiaFlowRouter router,
        ILogger<ChatController> logger)
    {
        _router = router;
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

            // Procesar con el router FresiaFlow
            var response = await _router.ProcessMessageAsync(
                request.Message,
                history,
                cancellationToken);

            return Ok(new
            {
                content = response.Content,
                agent = response.SelectedAgent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje del chat");
            return StatusCode(500, new { error = "Error al procesar el mensaje. Por favor, inténtalo de nuevo." });
        }
    }
}

/// <summary>
/// Request para el chat.
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto>? History { get; set; }
}

/// <summary>
/// DTO para mensajes del historial.
/// </summary>
public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" o "assistant"
    public string Content { get; set; } = string.Empty;
}

