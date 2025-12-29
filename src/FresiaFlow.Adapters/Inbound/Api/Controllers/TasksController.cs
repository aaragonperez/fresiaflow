using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Adapters.Inbound.Api.Dtos;
using FresiaFlow.Domain.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para gestión de tareas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly IProposeDailyPlanUseCase _proposeDailyPlanUseCase;
    private readonly ICreateTaskUseCase _createTaskUseCase;
    private readonly ITaskManagementUseCase _taskManagementUseCase;

    public TasksController(
        IProposeDailyPlanUseCase proposeDailyPlanUseCase,
        ICreateTaskUseCase createTaskUseCase,
        ITaskManagementUseCase taskManagementUseCase)
    {
        _proposeDailyPlanUseCase = proposeDailyPlanUseCase;
        _createTaskUseCase = createTaskUseCase;
        _taskManagementUseCase = taskManagementUseCase;
    }

    /// <summary>
    /// Obtiene las tareas pendientes.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingTasks([FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var tasks = await _taskManagementUseCase.GetPendingTasksAsync(date, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Obtiene una tarea por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskManagementUseCase.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return NotFound();
        }
        return Ok(task);
    }

    /// <summary>
    /// Crea una nueva tarea. Envía notificación por WhatsApp si está habilitado.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto, CancellationToken cancellationToken)
    {
        var command = new CreateTaskCommand(
            dto.Title,
            dto.Description,
            dto.Priority,
            dto.DueDate,
            dto.RelatedInvoiceId,
            dto.RelatedTransactionId);

        var result = await _createTaskUseCase.ExecuteAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetTaskById),
            new { id = result.Task.Id },
            result.Task);
    }

    /// <summary>
    /// Completa una tarea.
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteTask(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _taskManagementUseCase.CompleteAsync(id, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Actualiza la prioridad de una tarea.
    /// </summary>
    [HttpPatch("{id}/priority")]
    public async Task<IActionResult> UpdateTaskPriority(Guid id, [FromBody] UpdatePriorityDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _taskManagementUseCase.UpdatePriorityAsync(id, (TaskPriority)dto.Priority, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Fija o desfija una tarea (toggle).
    /// </summary>
    [HttpPost("{id}/toggle-pin")]
    public async Task<IActionResult> ToggleTaskPin(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _taskManagementUseCase.TogglePinAsync(id, cancellationToken);
            var task = await _taskManagementUseCase.GetByIdAsync(id, cancellationToken);
            return Ok(new { isPinned = task?.IsPinned });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Desmarca una tarea como completada.
    /// </summary>
    [HttpPost("{id}/uncomplete")]
    public async Task<IActionResult> UncompleteTask(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _taskManagementUseCase.UncompleteAsync(id, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Elimina una tarea.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        await _taskManagementUseCase.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Genera un plan diario usando IA.
    /// </summary>
    [HttpPost("daily-plan")]
    public async Task<IActionResult> ProposeDailyPlan(
        [FromBody] ProposeDailyPlanDto dto,
        CancellationToken cancellationToken)
    {
        var command = new ProposeDailyPlanCommand(
            dto.Date,
            dto.IncludePendingInvoices,
            dto.IncludeUnreconciledTransactions);

        var result = await _proposeDailyPlanUseCase.ExecuteAsync(command, cancellationToken);

        return Ok(result);
    }
}

