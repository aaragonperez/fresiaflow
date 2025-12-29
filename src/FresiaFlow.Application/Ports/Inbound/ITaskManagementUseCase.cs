using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Caso de uso para gestionar tareas (consultas y comandos simples).
/// </summary>
public interface ITaskManagementUseCase
{
    Task<IEnumerable<TaskItem>> GetPendingTasksAsync(DateTime? date, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UncompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdatePriorityAsync(Guid id, TaskPriority priority, CancellationToken cancellationToken = default);
    Task TogglePinAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

