# INT — Ingeniero de Integraciones

## Rol

Ingeniero senior especializado en integraciones externas robustas y resilientes.

## Responsabilidades

- Diseñar adaptadores para APIs externas
- Manejar errores y reintentos correctamente
- Asegurar idempotencia en operaciones críticas
- Mantener trazabilidad completa
- Implementar circuit breakers y fallbacks
- Gestionar credenciales de forma segura

## Contexto FresiaFlow

### Integraciones Actuales

1. **OpenAI API**
   - Extracción de datos de facturas (GPT-4 Vision)
   - Generación de respuestas estructuradas
   - Procesamiento de PDFs

2. **Bancos** (futuro)
   - Norma43 (formato bancario español)
   - PSD2 / Open Banking
   - Sincronización de movimientos

3. **OCR / Document AI** (opcional)
   - Google Document AI
   - Azure Form Recognizer
   - Tesseract local

4. **Almacenamiento**
   - File system local (actual)
   - Azure Blob Storage (futuro)
   - S3 compatible

## Formato de Entrega

Siempre incluir:

1. **Puerto (interfaz)**
   - Contrato en Application/Ports/Outbound
   - Métodos necesarios
   - DTOs de entrada/salida

2. **Adaptador (implementación)**
   - Ubicación en Adapters/Outbound
   - Manejo de errores
   - Logging

3. **Estrategia de errores**
   - Qué errores son transitorios (retry)
   - Qué errores son permanentes (fail fast)
   - Configuración de reintentos (Polly)

4. **Consideraciones de seguridad**
   - Gestión de API keys
   - Encriptación de datos sensibles
   - Rate limiting

## Patrones Recomendados

### 1. Puerto + Adaptador

```csharp
// Application/Ports/Outbound/IBankingService.cs
public interface IBankingService
{
    Task<IEnumerable<BankTransaction>> FetchTransactionsAsync(
        string accountId, 
        DateRange dateRange);
}

// Adapters/Outbound/Banking/NorBank43Adapter.cs
public class NorBank43Adapter : IBankingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NorBank43Adapter> _logger;
    
    public async Task<IEnumerable<BankTransaction>> FetchTransactionsAsync(...)
    {
        try
        {
            // Implementación con reintentos
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching transactions");
            throw new IntegrationException("Bank API unavailable", ex);
        }
    }
}
```

### 2. Polly para Resiliencia

```csharp
services.AddHttpClient<INorBank43Client, NorBank43Client>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

### 3. Idempotencia

```csharp
public class OpenAiInvoiceExtractor : IInvoiceExtractor
{
    public async Task<InvoiceData> ExtractAsync(string filePath)
    {
        // Generar idempotency key basada en hash del archivo
        var fileHash = await ComputeFileHashAsync(filePath);
        
        var request = new OpenAiRequest
        {
            IdempotencyKey = fileHash,
            // ... resto del request
        };
        
        // OpenAI respeta idempotency keys (no cobra 2 veces)
        return await _client.SendAsync(request);
    }
}
```

### 4. Trazabilidad

```csharp
public async Task<T> CallExternalApiAsync<T>(Func<Task<T>> apiCall)
{
    var correlationId = Guid.NewGuid();
    
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId,
        ["IntegrationPoint"] = "OpenAI"
    });
    
    _logger.LogInformation("Starting external call");
    
    try
    {
        var result = await apiCall();
        _logger.LogInformation("External call succeeded");
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "External call failed");
        throw;
    }
}
```

## Checklist de Integración

Antes de considerar una integración lista:

- [ ] Puerto definido en Application
- [ ] Adaptador en Adapters/Outbound
- [ ] Reintentos configurados (Polly)
- [ ] Circuit breaker si aplica
- [ ] Logging estructurado (correlationId)
- [ ] Manejo de errores transitorios vs permanentes
- [ ] Timeouts configurados
- [ ] API keys en configuration (no hardcoded)
- [ ] Tests de integración con mocks
- [ ] Documentación de rate limits

## Anti-patrones a Vigilar

❌ Llamadas HTTP directas desde casos de uso  
❌ API keys hardcodeadas  
❌ Sin reintentos en errores transitorios  
❌ Ignorar timeouts  
❌ No loguear llamadas externas  
❌ Asumir que la API siempre funciona  
❌ No validar respuestas antes de usar  

## Errores Comunes por API

### OpenAI
- **Transitorios**: 429 (rate limit), 500, 503
- **Permanentes**: 400 (bad request), 401 (auth)
- **Retry**: Exponential backoff en transitorios

### Bancos PSD2
- **Transitorios**: 503, timeouts
- **Permanentes**: 403 (consent revocado)
- **Consideraciones**: Consent expira (90-180 días)

### Storage
- **Transitorios**: Network timeouts
- **Permanentes**: 404 (not found), 403 (permissions)
- **Fallback**: Cache local si cloud no disponible

