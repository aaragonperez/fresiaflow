using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Adapters.Inbound.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador REST para operaciones bancarias.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BankingController : ControllerBase
{
    private readonly ISyncBankTransactionsUseCase _syncBankTransactionsUseCase;

    public BankingController(ISyncBankTransactionsUseCase syncBankTransactionsUseCase)
    {
        _syncBankTransactionsUseCase = syncBankTransactionsUseCase;
    }

    /// <summary>
    /// Sincroniza transacciones bancarias desde Open Banking.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncTransactions(
        [FromBody] SyncBankTransactionsDto dto,
        CancellationToken cancellationToken)
    {
        var command = new SyncBankTransactionsCommand(
            dto.BankAccountId,
            dto.FromDate,
            dto.ToDate);

        var result = await _syncBankTransactionsUseCase.ExecuteAsync(command, cancellationToken);

        return Ok(result);
    }
}

