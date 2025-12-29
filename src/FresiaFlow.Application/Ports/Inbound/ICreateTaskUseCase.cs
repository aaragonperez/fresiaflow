using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Comando para crear una nueva tarea.
/// </summary>
public record CreateTaskCommand(
    string Title,
    string? Description = null,
    TaskPriority Priority = TaskPriority.Medium,
    DateTime? DueDate = null,
    Guid? RelatedInvoiceId = null,
    Guid? RelatedTransactionId = null);

/// <summary>
/// Resultado de creación de tarea.
/// </summary>
public record CreateTaskResult(TaskItem Task);

/// <summary>
/// Puerto de entrada para crear tareas.
/// </summary>
public interface ICreateTaskUseCase
{
    Task<CreateTaskResult> ExecuteAsync(CreateTaskCommand command, CancellationToken cancellationToken = default);
}

