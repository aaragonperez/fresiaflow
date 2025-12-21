using FresiaFlow.Domain.Banking;

namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto de salida para integración con proveedores de Open Banking (AIS).
/// </summary>
public interface IBankProvider
{
    /// <summary>
    /// Obtiene las cuentas bancarias del usuario.
    /// </summary>
    Task<List<BankAccount>> GetAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las transacciones de una cuenta en un rango de fechas.
    /// </summary>
    Task<List<BankTransaction>> GetTransactionsAsync(
        string accountId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si el proveedor está disponible y autenticado.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

