using FresiaFlow.Application.Ports.Outbound;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador para configuración y pruebas de WhatsApp.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly ITaskRepository _taskRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        IWhatsAppNotificationService whatsAppService,
        ITaskRepository taskRepository,
        IConfiguration configuration,
        ILogger<WhatsAppController> logger)
    {
        _whatsAppService = whatsAppService;
        _taskRepository = taskRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Verifica el estado de la configuración de WhatsApp.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var isConfigured = await _whatsAppService.IsConfiguredAsync();
        var isEnabled = _configuration.GetValue<bool>("WhatsApp:Enabled");

        return Ok(new
        {
            isConfigured,
            isEnabled,
            phoneNumberId = isConfigured ? "Configurado" : "No configurado",
            recipientPhone = _configuration["WhatsApp:RecipientPhone"] ?? "No configurado"
        });
    }

    /// <summary>
    /// Envía un mensaje de prueba a WhatsApp.
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTestMessage([FromBody] SendTestMessageRequest request)
    {
        try
        {
            var recipientPhone = string.IsNullOrWhiteSpace(request.RecipientPhone)
                ? _configuration["WhatsApp:RecipientPhone"]
                : request.RecipientPhone;

            if (string.IsNullOrWhiteSpace(recipientPhone))
            {
                return BadRequest(new { error = "Número de teléfono no configurado" });
            }

            var success = await _whatsAppService.SendTestMessageAsync(recipientPhone!);

            if (success)
            {
                return Ok(new { message = "Mensaje de prueba enviado exitosamente" });
            }
            else
            {
                return StatusCode(500, new { error = "Error enviando mensaje de prueba" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando mensaje de prueba de WhatsApp");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Envía un resumen de tareas pendientes por WhatsApp.
    /// </summary>
    [HttpPost("send-tasks-summary")]
    public async Task<IActionResult> SendTasksSummary()
    {
        try
        {
            var pendingTasks = await _taskRepository.GetPendingTasksAsync();

            await _whatsAppService.SendTasksSummaryAsync(pendingTasks);

            return Ok(new
            {
                message = "Resumen enviado exitosamente",
                taskCount = pendingTasks.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando resumen de tareas por WhatsApp");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record SendTestMessageRequest(string? RecipientPhone = null);

