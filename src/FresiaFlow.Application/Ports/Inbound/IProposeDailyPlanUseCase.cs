using FresiaFlow.Domain.Tasks;

namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Puerto de entrada para generar el plan diario usando IA.
/// </summary>
public interface IProposeDailyPlanUseCase
{
    /// <summary>
    /// Genera un plan diario de tareas usando OpenAI API.
    /// </summary>
    Task<DailyPlanResult> ExecuteAsync(ProposeDailyPlanCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Comando para proponer un plan diario.
/// </summary>
public record ProposeDailyPlanCommand(
    DateTime Date,
    bool IncludePendingInvoices = true,
    bool IncludeUnreconciledTransactions = true
);

/// <summary>
/// Resultado con el plan diario propuesto.
/// </summary>
public record DailyPlanResult(
    List<TaskItem> ProposedTasks,
    string Summary,
    List<string> Recommendations
);

