using System.Text.Json;
using FresiaFlow.Adapters.Outbound.OneDrive;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FresiaFlow.Adapters.Outbound.InvoiceSources;

/// <summary>
/// Servicio de sincronización de facturas desde OneDrive/SharePoint.
/// Adapta el OneDriveSyncService existente para implementar IInvoiceSourceSyncService.
/// </summary>
public class OneDriveInvoiceSourceSyncService : IInvoiceSourceSyncService
{
    private readonly FresiaFlowDbContext _dbContext;
    private readonly IOneDriveSyncService _oneDriveSyncService;
    private readonly ILogger<OneDriveInvoiceSourceSyncService> _logger;

    public OneDriveInvoiceSourceSyncService(
        FresiaFlowDbContext dbContext,
        IOneDriveSyncService oneDriveSyncService,
        ILogger<OneDriveInvoiceSourceSyncService> logger)
    {
        _dbContext = dbContext;
        _oneDriveSyncService = oneDriveSyncService;
        _logger = logger;
    }

    public async Task<InvoiceSourceConfig?> GetConfigAsync(Guid sourceId)
    {
        return await _dbContext.InvoiceSourceConfigs
            .FirstOrDefaultAsync(c => c.Id == sourceId && c.SourceType == InvoiceSourceType.OneDrive);
    }

    public async Task<InvoiceSourceConfig> SaveConfigAsync(InvoiceSourceConfig config)
    {
        if (config.SourceType != InvoiceSourceType.OneDrive)
        {
            throw new ArgumentException("El tipo de fuente debe ser OneDrive", nameof(config));
        }

        var existing = await _dbContext.InvoiceSourceConfigs.FindAsync(config.Id);
        if (existing == null)
        {
            _dbContext.InvoiceSourceConfigs.Add(config);
        }
        else
        {
            existing.UpdateConfig(config.Name, config.ConfigJson);
            if (config.Enabled)
                existing.Enable();
            else
                existing.Disable();
        }

        await _dbContext.SaveChangesAsync();
        return config;
    }

    public async Task<SyncPreview> GetSyncPreviewAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var preview = new SyncPreview();

        try
        {
            var config = await GetConfigAsync(sourceId);
            if (config == null || !config.Enabled)
            {
                preview.ErrorMessage = "Configuración de OneDrive no encontrada o deshabilitada";
                return preview;
            }

            var oneDriveConfig = DeserializeOneDriveConfig(config.ConfigJson);
            if (oneDriveConfig == null)
            {
                preview.ErrorMessage = "Configuración de OneDrive inválida";
                return preview;
            }

            // Usar el servicio de OneDrive para obtener el preview
            // Necesitamos temporalmente actualizar la configuración de OneDrive para usar el preview
            // Esto es un workaround hasta que migremos completamente
            var tempOneDriveConfigDto = new OneDriveSyncConfigDto
            {
                Enabled = config.Enabled,
                TenantId = oneDriveConfig.TenantId,
                ClientId = oneDriveConfig.ClientId,
                ClientSecret = oneDriveConfig.ClientSecret,
                FolderPath = oneDriveConfig.FolderPath,
                DriveId = oneDriveConfig.DriveId,
                SyncIntervalMinutes = oneDriveConfig.SyncIntervalMinutes
            };

            // Guardar temporalmente la configuración en OneDriveSyncConfig para el preview
            await _oneDriveSyncService.SaveConfigAsync(tempOneDriveConfigDto);
            
            // Obtener preview
            var oneDrivePreview = await _oneDriveSyncService.GetSyncPreviewAsync(cancellationToken);
            
            // Mapear resultado
            preview.TotalFiles = oneDrivePreview.TotalFiles;
            preview.SupportedFiles = oneDrivePreview.SupportedFiles;
            preview.AlreadySynced = oneDrivePreview.AlreadySynced;
            preview.PendingToProcess = oneDrivePreview.PendingToProcess;
            preview.ErrorMessage = oneDrivePreview.ErrorMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo preview de sincronización de OneDrive");
            preview.ErrorMessage = ex.Message;
        }

        return preview;
    }

    public async Task<SyncResult> SyncNowAsync(Guid sourceId, bool forceReprocess = false, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult();
        var config = await GetConfigAsync(sourceId);

        if (config == null || !config.Enabled)
        {
            result.Success = false;
            result.ErrorMessage = "Configuración de OneDrive no encontrada o deshabilitada";
            return result;
        }

        var oneDriveConfig = DeserializeOneDriveConfig(config.ConfigJson);
        if (oneDriveConfig == null)
        {
            result.Success = false;
            result.ErrorMessage = "Configuración de OneDrive inválida";
            return result;
        }

        try
        {
            _logger.LogInformation("Iniciando sincronización de OneDrive: {SourceName}", config.Name);

            // Actualizar temporalmente la configuración de OneDrive para la sincronización
            var tempOneDriveConfigDto = new OneDriveSyncConfigDto
            {
                Enabled = config.Enabled,
                TenantId = oneDriveConfig.TenantId,
                ClientId = oneDriveConfig.ClientId,
                ClientSecret = oneDriveConfig.ClientSecret,
                FolderPath = oneDriveConfig.FolderPath,
                DriveId = oneDriveConfig.DriveId,
                SyncIntervalMinutes = oneDriveConfig.SyncIntervalMinutes
            };

            await _oneDriveSyncService.SaveConfigAsync(tempOneDriveConfigDto);

            // Ejecutar sincronización usando el servicio de OneDrive
            var oneDriveResult = await _oneDriveSyncService.SyncNowAsync(forceReprocess, cancellationToken);

            // Mapear resultado (OneDrive tiene campos adicionales que se pueden ignorar)
            result.Success = oneDriveResult.Success;
            result.ProcessedCount = oneDriveResult.ProcessedCount;
            result.FailedCount = oneDriveResult.FailedCount;
            result.SkippedCount = oneDriveResult.SkippedCount + (oneDriveResult.AlreadySynced + oneDriveResult.AlreadyExisted); // Combinar todos los omitidos
            result.ErrorMessage = oneDriveResult.ErrorMessage;
            result.DetailedErrors = oneDriveResult.DetailedErrors ?? new List<string>();

            // Actualizar estadísticas de la fuente
            if (result.Success)
            {
                config.RecordSuccessfulSync(result.ProcessedCount);
            }
            else
            {
                config.RecordFailedSync(result.ErrorMessage ?? "Error desconocido");
            }

            // Actualizar SyncedFiles que tienen Source = "OneDrive" para usar el formato "OneDrive-{sourceId}"
            // Solo actualizar los archivos sincronizados recientemente (últimos 5 minutos) para evitar actualizar todos los históricos
            var recentSyncTime = DateTime.UtcNow.AddMinutes(-5);
            var syncedFilesToUpdate = await _dbContext.SyncedFiles
                .Where(s => s.Source == "OneDrive" && s.SyncedAt >= recentSyncTime)
                .ToListAsync(cancellationToken);
            
            foreach (var syncedFile in syncedFilesToUpdate)
            {
                var newSource = $"OneDrive-{sourceId}";
                // Verificar si ya existe un registro con el nuevo Source y el mismo ExternalId
                var existingWithNewSource = await _dbContext.SyncedFiles
                    .FirstOrDefaultAsync(s => s.Source == newSource && s.ExternalId == syncedFile.ExternalId, cancellationToken);
                
                if (existingWithNewSource != null && existingWithNewSource.Id != syncedFile.Id)
                {
                    // Ya existe un registro con el nuevo Source, eliminar el duplicado antiguo
                    _logger.LogWarning("Eliminando registro duplicado con Source antiguo para ExternalId: {ExternalId}", syncedFile.ExternalId);
                    _dbContext.SyncedFiles.Remove(syncedFile);
                }
                else
                {
                    syncedFile.UpdateSource(newSource);
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                // Clave duplicada: puede ocurrir si hay sincronizaciones simultáneas
                // Intentar actualizar solo los que no causen conflicto
                _logger.LogWarning("Error de clave duplicada al actualizar Sources, intentando actualización parcial...");
                _dbContext.ChangeTracker.Clear();
                
                // Reintentar solo con los archivos que no causen conflicto
                var filesToRetry = await _dbContext.SyncedFiles
                    .Where(s => s.Source == "OneDrive" && s.SyncedAt >= recentSyncTime)
                    .ToListAsync(cancellationToken);
                
                foreach (var syncedFile in filesToRetry)
                {
                    var newSource = $"OneDrive-{sourceId}";
                    var existingWithNewSource = await _dbContext.SyncedFiles
                        .FirstOrDefaultAsync(s => s.Source == newSource && s.ExternalId == syncedFile.ExternalId, cancellationToken);
                    
                    if (existingWithNewSource == null)
                    {
                        syncedFile.UpdateSource(newSource);
                    }
                    else if (existingWithNewSource.Id != syncedFile.Id)
                    {
                        _dbContext.SyncedFiles.Remove(syncedFile);
                    }
                }
                
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante sincronización de OneDrive");
            config?.RecordFailedSync(ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            await _dbContext.SaveChangesAsync();
        }

        return result;
    }

    public async Task<SourceValidationResult> ValidateConfigAsync(string configJson, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = DeserializeOneDriveConfig(configJson);
            if (config == null)
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Configuración JSON inválida"
                };
            }

            // Validar que los campos requeridos estén presentes
            if (string.IsNullOrWhiteSpace(config.TenantId))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "TenantId es requerido"
                };
            }

            if (string.IsNullOrWhiteSpace(config.ClientId))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "ClientId es requerido"
                };
            }

            if (string.IsNullOrWhiteSpace(config.ClientSecret))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "ClientSecret es requerido"
                };
            }

            if (string.IsNullOrWhiteSpace(config.FolderPath))
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "FolderPath es requerido"
                };
            }

            // Usar el servicio de OneDrive para validar la configuración
            var folderInfo = await _oneDriveSyncService.ValidateAndGetFolderInfoAsync(
                config.TenantId,
                config.ClientId,
                config.ClientSecret,
                config.FolderPath,
                config.DriveId);

            if (folderInfo == null || !folderInfo.IsValid)
            {
                return new SourceValidationResult
                {
                    IsValid = false,
                    ErrorMessage = folderInfo?.ErrorMessage ?? "No se pudo conectar con OneDrive"
                };
            }

            return new SourceValidationResult
            {
                IsValid = true,
                Info = new Dictionary<string, object>
                {
                    { "folderPath", folderInfo.FolderPath ?? config.FolderPath },
                    { "fileCount", folderInfo.FileCount },
                    { "invoiceFileCount", folderInfo.InvoiceFileCount }
                }
            };
        }
        catch (Exception ex)
        {
            return new SourceValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error de validación: {ex.Message}"
            };
        }
    }

    private OneDriveConfig? DeserializeOneDriveConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<OneDriveConfig>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializando configuración de OneDrive");
            return null;
        }
    }

    /// <summary>
    /// Clase para deserializar la configuración JSON de OneDrive desde InvoiceSourceConfig.
    /// </summary>
    private class OneDriveConfig
    {
        public string TenantId { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public string? DriveId { get; set; }
        public int SyncIntervalMinutes { get; set; } = 15;
    }
}

