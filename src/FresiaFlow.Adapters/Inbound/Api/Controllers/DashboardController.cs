using FresiaFlow.Application.Ports.Inbound;
using Microsoft.AspNetCore.Mvc;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para el Dashboard.
/// Proporciona datos agregados para la vista principal del sistema.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardUseCase _dashboardUseCase;

    public DashboardController(IDashboardUseCase dashboardUseCase)
    {
        _dashboardUseCase = dashboardUseCase;
    }

    /// <summary>
    /// Obtiene todas las tareas pendientes del dashboard.
    /// Combina tareas almacenadas en BD con tareas generadas dinámicamente.
    /// </summary>
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(CancellationToken cancellationToken)
    {
        var result = await _dashboardUseCase.GetTasksAsync(cancellationToken);
        return Ok(result.Tasks);
    }

    /// <summary>
    /// Obtiene el resumen de saldos bancarios.
    /// </summary>
    [HttpGet("bank-balances")]
    public async Task<IActionResult> GetBankBalances(CancellationToken cancellationToken)
    {
        var summary = await _dashboardUseCase.GetBankBalancesAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Obtiene todas las alertas activas.
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken cancellationToken)
    {
        var alerts = await _dashboardUseCase.GetAlertsAsync(cancellationToken);
        return Ok(alerts);
    }
}

