using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para persistencia de tareas.
/// </summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TaskItem>> GetPendingTasksAsync(DateTime? date = null, CancellationToken cancellationToken = default);
    Task<List<TaskItem>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default);
    Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

