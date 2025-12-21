using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Banking;
using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Adapters.Outbound.Banking;

/// <summary>
/// Adapter para integración con proveedores de Open Banking (AIS).
/// Puede implementar múltiples proveedores (TrueLayer, Plaid, etc.).
/// </summary>
public class BankProviderAdapter : IBankProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _providerName;
    private readonly string _apiKey;

    public BankProviderAdapter(HttpClient httpClient, string providerName, string apiKey)
    {
        _httpClient = httpClient;
        _providerName = providerName;
        _apiKey = apiKey;
    }

    public async Task<List<BankAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implementar llamada real al proveedor Open Banking
        // Ejemplo con TrueLayer:
        // GET /data/v1/accounts
        // Authorization: Bearer {token}
        
        await Task.CompletedTask;
        
        // Stub: retorna lista vacía
        return new List<BankAccount>();
    }

    public async Task<List<BankTransaction>> GetTransactionsAsync(
        string accountId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implementar llamada real
        // Ejemplo:
        // GET /data/v1/accounts/{accountId}/transactions?from={fromDate}&to={toDate}
        
        await Task.CompletedTask;
        
        // Stub: retorna lista vacía
        return new List<BankTransaction>();
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Verificar conectividad y autenticación con el proveedor
        try
        {
            // Ejemplo: Health check endpoint
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private BankTransaction MapToDomainTransaction(object externalTransaction)
    {
        // TODO: Mapear transacción del formato del proveedor al dominio
        // Esto depende del proveedor específico (TrueLayer, Plaid, etc.)
        throw new NotImplementedException();
    }
}

