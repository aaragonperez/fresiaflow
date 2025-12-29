using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Adapters.Outbound.InvoiceSources;

/// <summary>
/// Caso de uso para sincronizar facturas desde todas las fuentes habilitadas.
/// </summary>
public class SyncInvoicesFromSourcesUseCase
{
    private readonly FresiaFlowDbContext _dbContext;
    private readonly InvoiceSourceSyncServiceFactory _serviceFactory;
    private readonly ILogger<SyncInvoicesFromSourcesUseCase> _logger;

    public SyncInvoicesFromSourcesUseCase(
        FresiaFlowDbContext dbContext,
        InvoiceSourceSyncServiceFactory serviceFactory,
        ILogger<SyncInvoicesFromSourcesUseCase> logger)
    {
        _dbContext = dbContext;
        _serviceFactory = serviceFactory;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza facturas desde todas las fuentes habilitadas.
    /// </summary>
    public async Task<Dictionary<Guid, SyncResult>> ExecuteAsync(bool forceReprocess = false, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<Guid, SyncResult>();

        // Obtener todas las fuentes habilitadas
        var enabledSources = await _dbContext.InvoiceSourceConfigs
            .Where(c => c.Enabled)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Iniciando sincronización de {Count} fuentes habilitadas", enabledSources.Count);

        // Procesar cada fuente usando el factory
        foreach (var source in enabledSources)
        {
            try
            {
                var service = _serviceFactory.GetService(source.SourceType);
                var result = await service.SyncNowAsync(source.Id, forceReprocess, cancellationToken);
                results[source.Id] = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando fuente {SourceId} ({SourceName})", source.Id, source.Name);
                results[source.Id] = new SyncResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        return results;
    }
}

