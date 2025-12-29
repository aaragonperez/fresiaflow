using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Tasks;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Caso de uso para crear una nueva tarea.
/// Envía notificación por WhatsApp si está habilitado.
/// </summary>
public class CreateTaskUseCase : ICreateTaskUseCase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly ILogger<CreateTaskUseCase> _logger;

    public CreateTaskUseCase(
        ITaskRepository taskRepository,
        IWhatsAppNotificationService whatsAppService,
        ILogger<CreateTaskUseCase> logger)
    {
        _taskRepository = taskRepository;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<CreateTaskResult> ExecuteAsync(
        CreateTaskCommand command, 
        CancellationToken cancellationToken = default)
    {
        // Validación
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new ArgumentException("El título de la tarea no puede estar vacío");
        }

        // Crear tarea
        var task = new TaskItem(
            command.Title,
            command.Description,
            command.Priority,
            command.DueDate);

        // Vincular a factura o transacción si corresponde
        if (command.RelatedInvoiceId.HasValue)
        {
            task.LinkToInvoice(command.RelatedInvoiceId.Value);
        }

        if (command.RelatedTransactionId.HasValue)
        {
            task.LinkToTransaction(command.RelatedTransactionId.Value);
        }

        // Guardar en base de datos
        await _taskRepository.AddAsync(task, cancellationToken);

        _logger.LogInformation(
            "Tarea creada: {TaskId} - {Title} (Prioridad: {Priority})", 
            task.Id, 
            task.Title, 
            task.Priority);

        // Enviar notificación de WhatsApp si está habilitado
        // El servicio de WhatsApp maneja internamente si está habilitado o no
        try
        {
            await _whatsAppService.SendTaskNotificationAsync(task, cancellationToken);
        }
        catch (Exception ex)
        {
            // No fallar si la notificación falla
            _logger.LogWarning(ex, "Error enviando notificación de WhatsApp para tarea {TaskId}", task.Id);
        }

        return new CreateTaskResult(task);
    }
}

