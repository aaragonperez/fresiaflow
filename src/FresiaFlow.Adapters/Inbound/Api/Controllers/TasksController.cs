using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Adapters.Inbound.Api.Dtos;
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

    public TasksController(IProposeDailyPlanUseCase proposeDailyPlanUseCase)
    {
        _proposeDailyPlanUseCase = proposeDailyPlanUseCase;
    }

    /// <summary>
    /// Obtiene las tareas pendientes.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingTasks([FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        // TODO: Implementar caso de uso para obtener tareas pendientes
        // Por ahora retorna lista vacía
        return Ok(new List<object>());
    }

    /// <summary>
    /// Obtiene una tarea por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(Guid id, CancellationToken cancellationToken)
    {
        // TODO: Implementar
        return NotFound();
    }

    /// <summary>
    /// Crea una nueva tarea.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementar caso de uso
        return Ok(new { id = Guid.NewGuid(), title = dto.Title });
    }

    /// <summary>
    /// Completa una tarea.
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteTask(Guid id, CancellationToken cancellationToken)
    {
        // TODO: Implementar
        return NoContent();
    }

    /// <summary>
    /// Actualiza la prioridad de una tarea.
    /// </summary>
    [HttpPatch("{id}/priority")]
    public async Task<IActionResult> UpdateTaskPriority(Guid id, [FromBody] UpdatePriorityDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementar
        return NoContent();
    }

    /// <summary>
    /// Elimina una tarea.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        // TODO: Implementar
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

