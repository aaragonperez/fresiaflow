namespace FresiaFlow.Application.Ports.Inbound;

/// <summary>
/// Consultas de dashboard agregadas para evitar lógica en controladores.
/// </summary>
public interface IDashboardUseCase
{
    Task<DashboardTasksResult> GetTasksAsync(CancellationToken cancellationToken = default);
    Task<BankSummaryDto> GetBankBalancesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default);
}

public record DashboardTaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Type,
    string Priority,
    string Status,
    bool IsPinned,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Dictionary<string, object>? Metadata);

public record DashboardTasksResult(IReadOnlyCollection<DashboardTaskDto> Tasks);

public record BankBalanceDto(
    Guid BankId,
    string BankName,
    string? AccountNumber,
    decimal Balance,
    string Currency,
    DateTime? LastMovementDate,
    decimal? LastMovementAmount);

public record BankSummaryDto(
    IReadOnlyCollection<BankBalanceDto> Banks,
    decimal TotalBalance,
    string PrimaryCurrency,
    decimal? PreviousDayBalance,
    decimal? PreviousDayVariation,
    decimal? PreviousMonthBalance,
    decimal? PreviousMonthVariation);

public record AlertDto(
    Guid Id,
    string Type,
    string Severity,
    string Title,
    string Description,
    DateTime OccurredAt,
    DateTime? AcknowledgedAt,
    DateTime? ResolvedAt,
    Dictionary<string, object>? Metadata);

