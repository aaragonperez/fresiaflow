using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Application.UseCases;

/// <summary>
/// Gestión de tareas a través del puerto de repositorio.
/// Encapsula operaciones sencillas para mantener los controladores delgados.
/// </summary>
public class TaskManagementUseCase : ITaskManagementUseCase
{
    private readonly ITaskRepository _taskRepository;

    public TaskManagementUseCase(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<IEnumerable<TaskItem>> GetPendingTasksAsync(DateTime? date, CancellationToken cancellationToken = default) =>
        await _taskRepository.GetPendingTasksAsync(date, cancellationToken);

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _taskRepository.GetByIdAsync(id, cancellationToken);

    public async Task CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await EnsureTaskAsync(id, cancellationToken);
        task.Complete();
        await _taskRepository.UpdateAsync(task, cancellationToken);
    }

    public async Task UncompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await EnsureTaskAsync(id, cancellationToken);
        task.Uncomplete();
        await _taskRepository.UpdateAsync(task, cancellationToken);
    }

    public async Task UpdatePriorityAsync(Guid id, TaskPriority priority, CancellationToken cancellationToken = default)
    {
        var task = await EnsureTaskAsync(id, cancellationToken);
        task.UpdatePriority(priority);
        await _taskRepository.UpdateAsync(task, cancellationToken);
    }

    public async Task TogglePinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await EnsureTaskAsync(id, cancellationToken);
        task.TogglePin();
        await _taskRepository.UpdateAsync(task, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _taskRepository.DeleteAsync(id, cancellationToken);

    private async Task<TaskItem> EnsureTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return task ?? throw new InvalidOperationException($"La tarea {id} no existe.");
    }
}

