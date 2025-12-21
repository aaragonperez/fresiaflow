namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para proponer un plan diario.
/// </summary>
public class ProposeDailyPlanDto
{
    public DateTime Date { get; set; }
    public bool IncludePendingInvoices { get; set; } = true;
    public bool IncludeUnreconciledTransactions { get; set; } = true;
}

