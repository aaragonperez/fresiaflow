using FresiaFlow.Adapters.Outbound.InvoiceSources;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Adapters.Outbound.OneDrive;

/// <summary>
/// Servicio en segundo plano que ejecuta la sincronización de OneDrive de forma periódica.
/// Ahora usa InvoiceSourceConfig en lugar de OneDriveSyncConfig.
/// </summary>
public class OneDriveSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OneDriveSyncBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Intervalo de verificación

    public OneDriveSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OneDriveSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de sincronización de OneDrive iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSyncIfNeededAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el servicio de sincronización de OneDrive");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Servicio de sincronización de OneDrive detenido");
    }

    private async Task CheckAndSyncIfNeededAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FresiaFlowDbContext>();
        
        // Buscar todas las fuentes de OneDrive habilitadas
        var oneDriveSources = await dbContext.InvoiceSourceConfigs
            .Where(c => c.SourceType == InvoiceSourceType.OneDrive && c.Enabled)
            .ToListAsync(cancellationToken);
        
        if (oneDriveSources.Count == 0)
        {
            return;
        }

        // Procesar cada fuente de OneDrive
        foreach (var source in oneDriveSources)
        {
            try
            {
                // Deserializar configuración para obtener el intervalo de sincronización
                var configJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(source.ConfigJson);
                var syncIntervalMinutes = 15; // Default
                
                if (configJson.TryGetProperty("syncIntervalMinutes", out var intervalProp))
                {
                    syncIntervalMinutes = intervalProp.GetInt32();
                }

                // Verificar si es tiempo de sincronizar
                var nextSyncTime = source.LastSyncAt?.AddMinutes(syncIntervalMinutes) ?? DateTime.MinValue;
                
                if (DateTime.UtcNow < nextSyncTime)
                {
                    continue;
                }

                _logger.LogInformation("Iniciando sincronización automática de OneDrive: {SourceName}", source.Name);

                // Usar el servicio unificado de fuentes
                var serviceFactory = scope.ServiceProvider.GetRequiredService<InvoiceSourceSyncServiceFactory>();
                var syncService = serviceFactory.GetService(InvoiceSourceType.OneDrive);
                var result = await syncService.SyncNowAsync(source.Id, forceReprocess: false, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Sincronización automática completada para {SourceName}: {Processed} procesados, {Skipped} omitidos, {Failed} fallidos",
                        source.Name, result.ProcessedCount, result.SkippedCount, result.FailedCount);
                }
                else
                {
                    _logger.LogWarning("Sincronización automática falló para {SourceName}: {Error}", source.Name, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando fuente OneDrive {SourceId} ({SourceName})", source.Id, source.Name);
            }
        }
    }
}


