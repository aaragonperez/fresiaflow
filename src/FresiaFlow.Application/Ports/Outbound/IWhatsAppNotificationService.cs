using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para envío de notificaciones por WhatsApp.
/// </summary>
public interface IWhatsAppNotificationService
{
    /// <summary>
    /// Envía una notificación de tarea pendiente por WhatsApp.
    /// </summary>
    Task SendTaskNotificationAsync(TaskItem task, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Envía una notificación de resumen de tareas pendientes.
    /// </summary>
    Task SendTasksSummaryAsync(List<TaskItem> tasks, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica que el servicio esté configurado correctamente.
    /// </summary>
    Task<bool> IsConfiguredAsync();
    
    /// <summary>
    /// Envía un mensaje de prueba para verificar la configuración.
    /// </summary>
    Task<bool> SendTestMessageAsync(string recipientPhone, CancellationToken cancellationToken = default);
}

