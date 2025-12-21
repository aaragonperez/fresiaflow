namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO para sincronizar transacciones bancarias.
/// </summary>
public class SyncBankTransactionsDto
{
    public Guid BankAccountId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

