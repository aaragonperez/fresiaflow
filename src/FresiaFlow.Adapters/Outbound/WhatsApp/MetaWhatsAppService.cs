using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Adapters.Outbound.WhatsApp;

/// <summary>
/// Implementación del servicio de notificaciones WhatsApp usando Meta Business API.
/// </summary>
public class MetaWhatsAppService : IWhatsAppNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MetaWhatsAppService> _logger;
    private readonly IConfiguration _configuration;
    
    private string? PhoneNumberId => _configuration["WhatsApp:PhoneNumberId"];
    private string? AccessToken => _configuration["WhatsApp:AccessToken"];
    private string? RecipientPhone => _configuration["WhatsApp:RecipientPhone"];
    private bool IsEnabled => _configuration.GetValue<bool>("WhatsApp:Enabled");
    
    public MetaWhatsAppService(
        IHttpClientFactory httpClientFactory,
        ILogger<MetaWhatsAppService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("WhatsApp");
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        return !string.IsNullOrWhiteSpace(PhoneNumberId) && 
               !string.IsNullOrWhiteSpace(AccessToken) && 
               !string.IsNullOrWhiteSpace(RecipientPhone);
    }

    public async Task SendTaskNotificationAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Notificaciones de WhatsApp deshabilitadas");
            return;
        }
        
        // Verificar si se debe enviar notificación al crear tarea
        var sendOnCreation = _configuration.GetValue<bool>("WhatsApp:SendOnTaskCreation");
        if (!sendOnCreation)
        {
            _logger.LogDebug("Notificaciones de WhatsApp en creación de tareas deshabilitadas");
            return;
        }
        
        if (!await IsConfiguredAsync())
        {
            _logger.LogWarning("WhatsApp no está configurado correctamente. Saltando notificación.");
            return;
        }

        try
        {
            var priorityEmoji = task.Priority switch
            {
                TaskPriority.Urgent => "🔴",
                TaskPriority.High => "🟠",
                TaskPriority.Medium => "🟡",
                TaskPriority.Low => "🟢",
                _ => "⚪"
            };

            var message = $"{priorityEmoji} *Nueva Tarea Pendiente*\n\n" +
                         $"📋 *{task.Title}*\n\n" +
                         $"{task.Description ?? "Sin descripción"}\n\n" +
                         $"⏰ Prioridad: *{GetPriorityText(task.Priority)}*";

            if (task.DueDate.HasValue)
            {
                message += $"\n📅 Vencimiento: {task.DueDate.Value:dd/MM/yyyy}";
            }

            await SendTextMessageAsync(RecipientPhone!, message, cancellationToken);
            
            _logger.LogInformation("Notificación de tarea enviada por WhatsApp: {TaskTitle}", task.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de tarea por WhatsApp: {TaskId}", task.Id);
            // No lanzamos la excepción para no interrumpir el flujo principal
        }
    }

    public async Task SendTasksSummaryAsync(List<TaskItem> tasks, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Notificaciones de WhatsApp deshabilitadas");
            return;
        }
        
        if (!await IsConfiguredAsync())
        {
            _logger.LogWarning("WhatsApp no está configurado correctamente. Saltando notificación.");
            return;
        }

        if (tasks.Count == 0)
        {
            _logger.LogDebug("No hay tareas pendientes para notificar");
            return;
        }

        try
        {
            var urgentCount = tasks.Count(t => t.Priority == TaskPriority.Urgent);
            var highCount = tasks.Count(t => t.Priority == TaskPriority.High);
            var mediumCount = tasks.Count(t => t.Priority == TaskPriority.Medium);
            var lowCount = tasks.Count(t => t.Priority == TaskPriority.Low);

            var message = $"📊 *Resumen de Tareas Pendientes*\n\n" +
                         $"Total: *{tasks.Count}* tareas\n\n";

            if (urgentCount > 0)
                message += $"🔴 Urgente: {urgentCount}\n";
            if (highCount > 0)
                message += $"🟠 Alta: {highCount}\n";
            if (mediumCount > 0)
                message += $"🟡 Media: {mediumCount}\n";
            if (lowCount > 0)
                message += $"🟢 Baja: {lowCount}\n";

            // Agregar las 3 tareas más urgentes
            var topTasks = tasks
                .OrderBy(t => (int)t.Priority)
                .ThenBy(t => t.DueDate)
                .Take(3)
                .ToList();

            if (topTasks.Any())
            {
                message += $"\n*Tareas prioritarias:*\n";
                for (int i = 0; i < topTasks.Count; i++)
                {
                    var task = topTasks[i];
                    var emoji = task.Priority switch
                    {
                        TaskPriority.Urgent => "🔴",
                        TaskPriority.High => "🟠",
                        _ => "🟡"
                    };
                    message += $"\n{i + 1}. {emoji} {task.Title}";
                }
            }

            await SendTextMessageAsync(RecipientPhone!, message, cancellationToken);
            
            _logger.LogInformation("Resumen de tareas enviado por WhatsApp: {Count} tareas", tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando resumen de tareas por WhatsApp");
        }
    }

    public async Task<bool> SendTestMessageAsync(string recipientPhone, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = "✅ *Prueba de Conexión Exitosa*\n\n" +
                         "FresiaFlow está correctamente configurado para enviar notificaciones por WhatsApp.\n\n" +
                         $"🕐 {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            await SendTextMessageAsync(recipientPhone, message, cancellationToken);
            
            _logger.LogInformation("Mensaje de prueba enviado exitosamente a {Phone}", recipientPhone);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando mensaje de prueba a {Phone}", recipientPhone);
            return false;
        }
    }

    private async Task SendTextMessageAsync(string recipientPhone, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(PhoneNumberId) || string.IsNullOrWhiteSpace(AccessToken))
        {
            throw new InvalidOperationException("WhatsApp no está configurado correctamente");
        }

        // Limpiar el número de teléfono (remover espacios, guiones, etc)
        var cleanPhone = new string(recipientPhone.Where(char.IsDigit).ToArray());
        
        // Asegurarse de que tenga el código de país (agregar 56 para Chile si no lo tiene)
        if (!cleanPhone.StartsWith("56") && cleanPhone.Length == 9)
        {
            cleanPhone = "56" + cleanPhone;
        }

        var url = $"https://graph.facebook.com/v18.0/{PhoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = cleanPhone,
            type = "text",
            text = new
            {
                preview_url = false,
                body = message
            }
        };

        var jsonContent = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Error enviando mensaje de WhatsApp. Status: {Status}, Error: {Error}", 
                response.StatusCode, 
                errorContent);
            
            throw new Exception($"Error enviando mensaje de WhatsApp: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("Mensaje enviado exitosamente. Respuesta: {Response}", responseContent);
    }

    private static string GetPriorityText(TaskPriority priority)
    {
        return priority switch
        {
            TaskPriority.Urgent => "Urgente",
            TaskPriority.High => "Alta",
            TaskPriority.Medium => "Media",
            TaskPriority.Low => "Baja",
            _ => "Desconocida"
        };
    }
}

