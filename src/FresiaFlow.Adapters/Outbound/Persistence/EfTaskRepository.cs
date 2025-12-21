using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Implementación del repositorio de tareas usando EF Core.
/// </summary>
public class EfTaskRepository : ITaskRepository
{
    private readonly FresiaFlowDbContext _context;

    public EfTaskRepository(FresiaFlowDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<TaskItem>> GetPendingTasksAsync(DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.Where(t => !t.IsCompleted);

        if (date.HasValue)
        {
            query = query.Where(t => !t.DueDate.HasValue || t.DueDate.Value.Date == date.Value.Date);
        }

        return await query
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TaskItem>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Where(t => t.Priority == priority && !t.IsCompleted)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(task, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetByIdAsync(id, cancellationToken);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

