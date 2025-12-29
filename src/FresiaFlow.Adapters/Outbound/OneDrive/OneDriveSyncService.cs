using System.IO;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Application.Ports.Inbound;
using FresiaFlow.Application.Ports.Outbound;
using FresiaFlow.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FresiaFlow.Adapters.Outbound.OneDrive;

public interface IOneDriveSyncService
{
    Task<OneDriveSyncConfig?> GetConfigAsync();
    Task<OneDriveSyncConfig> SaveConfigAsync(OneDriveSyncConfigDto config);
    Task<SyncPreview> GetSyncPreviewAsync(CancellationToken cancellationToken = default);
    Task<SyncResult> SyncNowAsync(bool forceReprocess = false, CancellationToken cancellationToken = default);
    Task<List<SyncedFile>> GetSyncedFilesAsync(int page = 1, int pageSize = 50);
    Task<OneDriveFolderInfo?> ValidateAndGetFolderInfoAsync(string tenantId, string clientId, string clientSecret, string folderPath, string? driveId = null);
    Task<(Stream FileStream, string ContentType, string FileName)?> DownloadFileAsync(Guid syncedFileId);
    Task ClearAllDataAsync();
    void CancelCurrentSync();
    void PauseCurrentSync();
    void ResumeCurrentSync();
    bool IsSyncInProgress { get; }
    bool IsSyncPaused { get; }
}

public class OneDriveSyncService : IOneDriveSyncService
{
    private readonly FresiaFlowDbContext _dbContext;
    private readonly IUploadInvoiceUseCase _uploadInvoiceUseCase;
    private readonly ILogger<OneDriveSyncService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ISyncProgressNotifier _progressNotifier;

    private static readonly string[] SupportedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    
    private CancellationTokenSource? _currentSyncCts;
    private bool _syncInProgress;
    private bool _syncPaused;
    private readonly object _syncLock = new object();
    
    // Token management - para auto-refresh cuando expire
    private string? _currentAccessToken;
    private DateTime _tokenExpiration = DateTime.MinValue;
    private string? _currentTenantId;
    private string? _currentClientId;
    private string? _currentClientSecret;

    public bool IsSyncInProgress 
    { 
        get 
        { 
            lock (_syncLock) 
            { 
                return _syncInProgress; 
            } 
        } 
    }

    public bool IsSyncPaused 
    { 
        get 
        { 
            lock (_syncLock) 
            { 
                return _syncPaused; 
            } 
        } 
    }

    public OneDriveSyncService(
        FresiaFlowDbContext dbContext,
        IUploadInvoiceUseCase uploadInvoiceUseCase,
        ILogger<OneDriveSyncService> logger,
        IHttpClientFactory httpClientFactory,
        ISyncProgressNotifier progressNotifier)
    {
        _dbContext = dbContext;
        _uploadInvoiceUseCase = uploadInvoiceUseCase;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("OneDrive");
        _progressNotifier = progressNotifier;
    }

    public void CancelCurrentSync()
    {
        lock (_syncLock)
        {
            if (_currentSyncCts != null && !_currentSyncCts.IsCancellationRequested)
            {
                _logger.LogWarning("Cancelando sincronización manual por solicitud del usuario");
                _currentSyncCts.Cancel();
                _syncPaused = false;
            }
        }
    }

    public void PauseCurrentSync()
    {
        lock (_syncLock)
        {
            if (_syncInProgress && !_syncPaused)
            {
                _logger.LogInformation("Pausando sincronización por solicitud del usuario");
                _syncPaused = true;
            }
        }
    }

    public void ResumeCurrentSync()
    {
        lock (_syncLock)
        {
            if (_syncInProgress && _syncPaused)
            {
                _logger.LogInformation("Reanudando sincronización por solicitud del usuario");
                _syncPaused = false;
            }
        }
    }

    public async Task<OneDriveSyncConfig?> GetConfigAsync()
    {
        return await _dbContext.OneDriveSyncConfigs.FirstOrDefaultAsync();
    }

    public async Task<OneDriveSyncConfig> SaveConfigAsync(OneDriveSyncConfigDto dto)
    {
        var config = await _dbContext.OneDriveSyncConfigs.FirstOrDefaultAsync();
        
        if (config == null)
        {
            config = OneDriveSyncConfig.CreateDefault();
            _dbContext.OneDriveSyncConfigs.Add(config);
        }

        // Solo actualizar ClientSecret si viene un valor no vacío
        // Esto evita sobrescribir un secret guardado cuando el frontend no lo envía
        if (!string.IsNullOrWhiteSpace(dto.ClientSecret))
        {
            config.UpdateCredentials(dto.TenantId, dto.ClientId, dto.ClientSecret);
        }
        else
        {
            // Si el ClientSecret está vacío, solo actualizar TenantId y ClientId
            config.UpdateCredentialsWithoutSecret(dto.TenantId, dto.ClientId);
        }
        
        config.UpdateFolderSettings(dto.FolderPath, dto.DriveId);
        config.SetSyncInterval(dto.SyncIntervalMinutes);

        if (dto.Enabled)
            config.Enable();
        else
            config.Disable();

        await _dbContext.SaveChangesAsync();
        return config;
    }

    public async Task<SyncPreview> GetSyncPreviewAsync(CancellationToken cancellationToken = default)
    {
        var preview = new SyncPreview();

        try
        {
            var config = await GetConfigAsync();
            if (config == null || !config.IsConfigured())
            {
                preview.ErrorMessage = "OneDrive no está configurado";
                return preview;
            }

            // Obtener token de acceso
            var accessToken = await GetAccessTokenAsync(config.TenantId!, config.ClientId!, config.ClientSecret!);
            if (string.IsNullOrEmpty(accessToken))
            {
                preview.ErrorMessage = "No se pudo obtener el token de acceso";
                return preview;
            }

            // Listar archivos en la carpeta configurada
            var allFiles = await ListFilesInFolderAsync(accessToken, config.FolderPath!, config.DriveId);
            preview.TotalFiles = allFiles.Count;

            // Clasificar archivos por extensión
            var filesByExt = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var unsupportedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in allFiles)
            {
                var ext = Path.GetExtension(file.Name).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = "(sin extensión)";

                if (!filesByExt.ContainsKey(ext))
                    filesByExt[ext] = 0;
                filesByExt[ext]++;

                if (!SupportedExtensions.Contains(ext))
                {
                    unsupportedExts.Add(ext);
                }
            }

            preview.FilesByExtension = filesByExt;
            preview.UnsupportedExtensions = unsupportedExts.ToList();

            // Filtrar solo archivos soportados
            var supportedFiles = allFiles.Where(f => 
                SupportedExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant())).ToList();
            
            preview.SupportedFiles = supportedFiles.Count;
            preview.UnsupportedFiles = preview.TotalFiles - preview.SupportedFiles;

            // Verificar cuántos ya están sincronizados
            var externalIds = supportedFiles.Select(f => f.Id).ToList();
            var syncedFileIds = await _dbContext.SyncedFiles
                .Where(s => s.Source == "OneDrive" && externalIds.Contains(s.ExternalId) && s.Status == SyncStatus.Completed)
                .Select(s => s.ExternalId)
                .ToListAsync(cancellationToken);

            preview.AlreadySynced = syncedFileIds.Count;

            // Verificar cuántos ya existen en la base de datos de facturas (por nombre de archivo)
            var pendingFileNames = supportedFiles
                .Where(f => !syncedFileIds.Contains(f.Id))
                .Select(f => f.Name)
                .ToList();

            var existingInvoiceCount = 0;
            foreach (var fileName in pendingFileNames)
            {
                var exists = await _dbContext.InvoicesReceived
                    .AnyAsync(i => i.OriginalFilePath.Contains(fileName), cancellationToken);
                if (exists) existingInvoiceCount++;
            }

            preview.AlreadyExistsInDb = existingInvoiceCount;
            preview.PendingToProcess = preview.SupportedFiles - preview.AlreadySynced - preview.AlreadyExistsInDb;

            _logger.LogInformation(
                "Preview de sincronización: Total={Total}, Soportados={Supported}, Ya sincronizados={Synced}, Ya en BD={InDb}, Pendientes={Pending}",
                preview.TotalFiles, preview.SupportedFiles, preview.AlreadySynced, preview.AlreadyExistsInDb, preview.PendingToProcess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo preview de sincronización");
            preview.ErrorMessage = ex.Message;
        }

        return preview;
    }

    public async Task<SyncResult> SyncNowAsync(bool forceReprocess = false, CancellationToken cancellationToken = default)
    {
        // Crear y registrar el CancellationTokenSource para sincronización manual
        CancellationTokenSource? linkedCts = null;
        
        lock (_syncLock)
        {
            if (_syncInProgress)
            {
                _logger.LogWarning("Ya hay una sincronización en progreso");
                return new SyncResult 
                { 
                    Success = false, 
                    ErrorMessage = "Ya hay una sincronización en progreso. Espera a que termine o cancélala primero." 
                };
            }
            
            _syncInProgress = true;
            _syncPaused = false;
            _currentSyncCts = new CancellationTokenSource();
            
            // Combinar el token pasado como parámetro con el token interno
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _currentSyncCts.Token);
        }

        var result = new SyncResult();
        var config = await GetConfigAsync();

        if (config == null || !config.IsConfigured())
        {
            result.Success = false;
            result.ErrorMessage = "OneDrive no está configurado correctamente";
            lock (_syncLock) 
            { 
                _syncInProgress = false;
                _syncPaused = false;
            }
            return result;
        }

        // Nota: No verificamos config.Enabled aquí porque la sincronización manual
        // debe funcionar independientemente del estado de la sincronización automática

        try
        {
            _logger.LogInformation("Iniciando sincronización con OneDrive (Forzar reproceso: {ForceReprocess})...", forceReprocess);
            
            // Obtener token de acceso
            var accessToken = await GetAccessTokenAsync(config.TenantId!, config.ClientId!, config.ClientSecret!);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("No se pudo obtener el token de acceso de Microsoft Graph");
            }
            // Listar archivos en la carpeta configurada
            var allFiles = await ListFilesInFolderAsync(accessToken, config.FolderPath!, config.DriveId);
            _logger.LogInformation("Encontrados {Count} archivos totales en OneDrive", allFiles.Count);

            // Filtrar solo archivos soportados y contar por tipo
            var filesByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var files = new List<OneDriveFile>();

            foreach (var file in allFiles)
            {
                var ext = Path.GetExtension(file.Name).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = "(sin extensión)";

                if (!filesByType.ContainsKey(ext))
                    filesByType[ext] = 0;
                filesByType[ext]++;

                if (SupportedExtensions.Contains(ext))
                {
                    files.Add(file);
                }
            }

            result.FilesByType = filesByType;
            result.TotalDetected = allFiles.Count;

            var totalFiles = files.Count;
            var processedFiles = 0;

            _logger.LogInformation("Archivos soportados a procesar: {Count} de {Total}", totalFiles, allFiles.Count);

            // Enviar progreso inicial
            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
            {
                CurrentFile = "Iniciando sincronización...",
                CurrentFileName = "Iniciando sincronización...",
                ProcessedCount = 0,
                TotalCount = totalFiles,
                Percentage = 0,
                Status = "syncing",
                Stage = "initializing",
                Message = $"Se encontraron {totalFiles} archivos soportados de {allFiles.Count} totales",
                ProcessedFiles = 0,
                FailedFiles = 0,
                SkippedFiles = 0,
                AlreadyExistedFiles = 0
            }, linkedCts!.Token);

            foreach (var file in files)
            {
                // Verificar si se solicitó la cancelación
                if (linkedCts!.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Sincronización cancelada por el usuario");
                    result.Success = false;
                    result.ErrorMessage = "Sincronización cancelada por el usuario";
                    
                    // Enviar progreso de cancelación
                    await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                    {
                        CurrentFile = "Cancelado",
                        ProcessedCount = processedFiles,
                        TotalCount = totalFiles,
                        Percentage = (int)((double)processedFiles / totalFiles * 100),
                        Status = "cancelled",
                        Message = "Sincronización cancelada por el usuario"
                    }, linkedCts.Token);
                    
                    break;
                }

                // Verificar si está pausado y esperar hasta que se reanude
                while (IsSyncPaused && !linkedCts!.Token.IsCancellationRequested)
                {
                    await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                    {
                        CurrentFile = file.Name,
                        ProcessedCount = processedFiles,
                        TotalCount = totalFiles,
                        Percentage = (int)((double)processedFiles / totalFiles * 100),
                        Status = "paused",
                        Message = "Sincronización pausada. Esperando reanudación..."
                    }, linkedCts.Token);
                    
                    await Task.Delay(500, linkedCts.Token); // Esperar 500ms antes de verificar de nuevo
                }

                // Si se canceló mientras estaba pausado, salir
                if (linkedCts!.Token.IsCancellationRequested)
                {
                    break;
                }
                
                try
                {
                    // Verificar si ya fue sincronizado
                    // Primero buscar por Source = "OneDrive" (compatibilidad con código existente)
                    var existingSync = await _dbContext.SyncedFiles
                        .FirstOrDefaultAsync(s => s.Source == "OneDrive" && s.ExternalId == file.Id, linkedCts.Token);

                    // Si no se encuentra, buscar por ExternalId en cualquier Source que empiece con "OneDrive"
                    // Esto previene duplicados cuando hay múltiples fuentes de OneDrive
                    if (existingSync == null)
                    {
                        existingSync = await _dbContext.SyncedFiles
                            .FirstOrDefaultAsync(s => s.ExternalId == file.Id && s.Source.StartsWith("OneDrive"), linkedCts.Token);
                    }

                    bool shouldProcess = false;

                    if (existingSync != null)
                    {
                        // Si ya existe y está completado
                        if (existingSync.Status == SyncStatus.Completed)
                        {
                            // Si el archivo ha cambiado (hash diferente), siempre reprocesar
                            if (existingSync.FileHash != file.Hash)
                            {
                                _logger.LogInformation("Archivo modificado detectado: {FileName}, reprocesando...", file.Name);
                                existingSync.UpdateHash(file.Hash, file.LastModified);
                                existingSync.MarkAsProcessing();
                                shouldProcess = true;
                            }
                            // Si NO ha cambiado pero se fuerza el reproceso
                            else if (forceReprocess)
                            {
                                _logger.LogInformation("Forzando reproceso de: {FileName}", file.Name);
                                existingSync.MarkAsProcessing();
                                shouldProcess = true;
                            }
                            // Si NO ha cambiado y NO se fuerza, saltar (inteligente!)
                            else
                            {
                                _logger.LogDebug("Saltando archivo ya procesado: {FileName}", file.Name);
                                result.AlreadySynced++;
                                
                                // Actualizar progreso para archivos omitidos también
                                processedFiles++;
                                var skipPct = (int)((double)processedFiles / totalFiles * 100);
                                await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                                {
                                    CurrentFile = file.Name,
                                    CurrentFileName = file.Name,
                                    ProcessedCount = processedFiles,
                                    TotalCount = totalFiles,
                                    Percentage = skipPct,
                                    Status = "syncing",
                                    Stage = "processing",
                                    Message = $"Ya sincronizado: {file.Name}",
                                    ProcessedFiles = result.ProcessedCount,
                                    FailedFiles = result.FailedCount,
                                    SkippedFiles = result.AlreadySynced + result.AlreadyExisted,
                                    AlreadyExistedFiles = result.AlreadyExisted,
                                    CurrentFileStatus = "skipped",
                                    CurrentFileSize = file.Size
                                }, linkedCts.Token);
                                continue;
                            }
                        }
                        // Si existe pero falló o está pendiente, reprocesar
                        else
                        {
                            _logger.LogInformation("Reprocesando archivo con estado {Status}: {FileName}", existingSync.Status, file.Name);
                            existingSync.UpdateHash(file.Hash, file.LastModified);
                            existingSync.MarkAsProcessing();
                            shouldProcess = true;
                        }
                    }
                    else
                    {
                        // Archivo nuevo - verificar si ya existe la factura en la base de datos
                        // Buscar por nombre de archivo en OriginalFilePath
                        var existingInvoice = await _dbContext.InvoicesReceived
                            .FirstOrDefaultAsync(i => i.OriginalFilePath.Contains(file.Name), linkedCts.Token);

                        if (existingInvoice != null && !forceReprocess)
                        {
                            // La factura ya existe, marcar como omitido
                            _logger.LogInformation("Factura ya existe para archivo: {FileName} (ID: {InvoiceId})", file.Name, existingInvoice.Id);
                            
                            // Si no existe un registro de sincronización, crear uno nuevo
                            if (existingSync == null)
                            {
                                existingSync = SyncedFile.Create(
                                    "OneDrive",
                                    file.Id,
                                    file.Name,
                                    file.Path,
                                    file.Size,
                                    file.Hash,
                                    file.LastModified
                                );
                                existingSync.MarkAsCompleted(existingInvoice.Id);
                                _dbContext.SyncedFiles.Add(existingSync);
                                
                                try
                                {
                                    await _dbContext.SaveChangesAsync(linkedCts.Token);
                                }
                                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                                {
                                    // Clave duplicada: otro proceso ya creó el registro, buscarlo y actualizarlo
                                    _logger.LogWarning("Registro duplicado detectado para {FileName}, buscando registro existente...", file.Name);
                                    var duplicateSync = await _dbContext.SyncedFiles
                                        .FirstOrDefaultAsync(s => s.ExternalId == file.Id && s.Source.StartsWith("OneDrive"), linkedCts.Token);
                                    
                                    if (duplicateSync != null)
                                    {
                                        existingSync = duplicateSync;
                                        if (existingSync.Status != SyncStatus.Completed || existingSync.InvoiceId != existingInvoice.Id)
                                        {
                                            existingSync.MarkAsCompleted(existingInvoice.Id);
                                            await _dbContext.SaveChangesAsync(linkedCts.Token);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogError("No se pudo encontrar el registro duplicado después del error");
                                        throw;
                                    }
                                }
                            }
                            else
                            {
                                // Ya existe el registro, actualizar si es necesario
                                if (existingSync.Status != SyncStatus.Completed || existingSync.InvoiceId != existingInvoice.Id)
                                {
                                    existingSync.MarkAsCompleted(existingInvoice.Id);
                                    await _dbContext.SaveChangesAsync(linkedCts.Token);
                                }
                            }
                            
                            result.AlreadyExisted++;
                            
                            processedFiles++;
                            var pct = (int)((double)processedFiles / totalFiles * 100);
                            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                            {
                                CurrentFile = file.Name,
                                CurrentFileName = file.Name,
                                ProcessedCount = processedFiles,
                                TotalCount = totalFiles,
                                Percentage = pct,
                                Status = "syncing",
                                Stage = "processing",
                                Message = $"Factura ya existente: {file.Name}",
                                ProcessedFiles = result.ProcessedCount,
                                FailedFiles = result.FailedCount,
                                SkippedFiles = result.AlreadySynced,
                                AlreadyExistedFiles = result.AlreadyExisted + 1,
                                CurrentFileStatus = "skipped",
                                CurrentFileSize = file.Size
                            }, linkedCts.Token);
                            continue;
                        }

                        // Archivo nuevo, procesar
                        _logger.LogInformation("Nuevo archivo detectado: {FileName}", file.Name);
                        
                        if (existingSync == null)
                        {
                            existingSync = SyncedFile.Create(
                                "OneDrive",
                                file.Id,
                                file.Name,
                                file.Path,
                                file.Size,
                                file.Hash,
                                file.LastModified
                            );
                            _dbContext.SyncedFiles.Add(existingSync);
                        }
                        else
                        {
                            // Si existe pero no estaba en el filtro original, actualizar sus datos
                            existingSync.UpdateHash(file.Hash, file.LastModified);
                            existingSync.MarkAsProcessing();
                        }
                        
                        shouldProcess = true;
                    }

                    try
                    {
                        await _dbContext.SaveChangesAsync(linkedCts.Token);
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        // Clave duplicada: otro proceso ya creó el registro, buscarlo y reutilizarlo
                        _logger.LogWarning("Registro duplicado detectado para {FileName}, buscando registro existente...", file.Name);
                        var duplicateSync = await _dbContext.SyncedFiles
                            .FirstOrDefaultAsync(s => s.ExternalId == file.Id && s.Source.StartsWith("OneDrive"), linkedCts.Token);
                        
                        if (duplicateSync != null)
                        {
                            existingSync = duplicateSync;
                            // Si el registro existente está completado y el hash no cambió, no procesar
                            if (existingSync.Status == SyncStatus.Completed && existingSync.FileHash == file.Hash && !forceReprocess)
                            {
                                shouldProcess = false;
                                result.AlreadySynced++;
                            }
                            else
                            {
                                existingSync.UpdateHash(file.Hash, file.LastModified);
                                if (existingSync.Status != SyncStatus.Processing)
                                {
                                    existingSync.MarkAsProcessing();
                                }
                                shouldProcess = true;
                                await _dbContext.SaveChangesAsync(linkedCts.Token);
                            }
                        }
                        else
                        {
                            _logger.LogError("No se pudo encontrar el registro duplicado después del error para {FileName}", file.Name);
                            throw;
                        }
                    }

                    // Solo procesar si es necesario
                    if (shouldProcess)
                    {
                        // Notificar inicio de descarga
                        await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                        {
                            CurrentFile = file.Name,
                            CurrentFileName = file.Name,
                            ProcessedCount = processedFiles,
                            TotalCount = totalFiles,
                            Percentage = (int)((double)processedFiles / totalFiles * 100),
                            Status = "syncing",
                            Stage = "downloading",
                            Message = $"Descargando: {file.Name}",
                            ProcessedFiles = result.ProcessedCount,
                            FailedFiles = result.FailedCount,
                            SkippedFiles = result.AlreadySynced + result.AlreadyExisted,
                            AlreadyExistedFiles = result.AlreadyExisted,
                            CurrentFileStatus = "downloading",
                            CurrentFileSize = file.Size
                        }, linkedCts!.Token);
                        
                        // Descargar y procesar el archivo
                        // Pasar también fileId y driveId para poder obtener nueva URL si expira
                        var fileContent = await DownloadFileAsync(accessToken, file.DownloadUrl, file.Id, config.DriveId);
                        
                        // Notificar inicio de procesamiento
                        await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                        {
                            CurrentFile = file.Name,
                            CurrentFileName = file.Name,
                            ProcessedCount = processedFiles,
                            TotalCount = totalFiles,
                            Percentage = (int)((double)processedFiles / totalFiles * 100),
                            Status = "syncing",
                            Stage = "extracting",
                            Message = $"Extrayendo datos: {file.Name}",
                            ProcessedFiles = result.ProcessedCount,
                            FailedFiles = result.FailedCount,
                            SkippedFiles = result.AlreadySynced + result.AlreadyExisted,
                            AlreadyExistedFiles = result.AlreadyExisted,
                            CurrentFileStatus = "extracting",
                            CurrentFileSize = file.Size
                        }, linkedCts!.Token);
                        
                        using var fileStream = new MemoryStream(fileContent);
                        var uploadCommand = new UploadInvoiceCommand(
                            fileStream,
                            file.Name,
                            GetContentType(file.Name));

                        // Procesar la factura con IA
                        var invoiceResult = await _uploadInvoiceUseCase.ExecuteAsync(uploadCommand);

                        existingSync.MarkAsCompleted(invoiceResult.InvoiceId);
                        result.ProcessedCount++;
                        _logger.LogInformation("Factura procesada con IA: {FileName} -> {InvoiceId}", file.Name, invoiceResult.InvoiceId);
                    }
                }
                catch (Exception ex)
                {
                    var syncFile = await _dbContext.SyncedFiles
                        .FirstOrDefaultAsync(s => s.Source == "OneDrive" && s.ExternalId == file.Id);
                    
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" -> {ex.InnerException.Message}";
                    }
                    
                    // Detectar errores críticos y pausar automáticamente
                    if (IsCriticalError(ex) || IsTokenError(ex))
                    {
                        var isUrlExpired = errorMessage.ToLowerInvariant().Contains("url de descarga ha expirado");
                        var isRateLimit = errorMessage.ToLowerInvariant().Contains("429") || errorMessage.ToLowerInvariant().Contains("rate limit");
                        var isPermissionError = errorMessage.ToLowerInvariant().Contains("403") || errorMessage.ToLowerInvariant().Contains("forbidden");
                        
                        _logger.LogError(ex, "Error crítico detectado durante sincronización. Pausando automáticamente...");
                        
                        // Pausar la sincronización
                        lock (_syncLock)
                        {
                            if (_syncInProgress && !_syncPaused)
                            {
                                _syncPaused = true;
                            }
                        }
                        
                        // Construir mensaje de error específico según el tipo
                        string criticalErrorMessage;
                        if (isUrlExpired)
                        {
                            criticalErrorMessage = "⚠️ URL de descarga expirada y no se pudo obtener una nueva. " +
                                "Esto puede deberse a: archivo muy grande, conexión lenta, o límites de Microsoft Graph. " +
                                "La sincronización ha sido pausada. Puedes reanudarla cuando la conexión mejore o más tarde.";
                        }
                        else if (isRateLimit)
                        {
                            criticalErrorMessage = "⚠️ Límite de solicitudes alcanzado (Rate Limiting) en Microsoft Graph. " +
                                "Microsoft está limitando las solicitudes. Espera unos minutos antes de reanudar. " +
                                "La sincronización ha sido pausada automáticamente.";
                        }
                        else if (isPermissionError)
                        {
                            criticalErrorMessage = "⚠️ Error de permisos con Microsoft Graph. " +
                                "La aplicación no tiene permisos suficientes para acceder a los archivos. " +
                                "Verifica los permisos en Azure AD (Files.Read.All, Sites.Read.All). " +
                                "La sincronización ha sido pausada automáticamente.";
                        }
                        else if (IsTokenError(ex))
                        {
                            criticalErrorMessage = "⚠️ Error de autenticación con Microsoft Graph. " +
                                "El token ha expirado o es inválido y no se pudo refrescar. " +
                                "Verifica la configuración en Azure AD (Client Secret, permisos, etc.). " +
                                "La sincronización ha sido pausada automáticamente.";
                        }
                        else
                        {
                            criticalErrorMessage = $"⚠️ Error crítico durante la sincronización: {errorMessage}. " +
                                "La sincronización ha sido pausada automáticamente. " +
                                "Revisa los logs para más detalles y reanuda cuando el problema se resuelva.";
                        }
                        
                        await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                        {
                            CurrentFile = file.Name,
                            CurrentFileName = file.Name,
                            ProcessedCount = processedFiles,
                            TotalCount = totalFiles,
                            Percentage = (int)((double)processedFiles / totalFiles * 100),
                            Status = "paused",
                            Stage = "processing",
                            Message = criticalErrorMessage,
                            ProcessedFiles = result.ProcessedCount,
                            FailedFiles = result.FailedCount,
                            SkippedFiles = result.AlreadySynced + result.AlreadyExisted,
                            AlreadyExistedFiles = result.AlreadyExisted,
                            CurrentFileStatus = "failed",
                            CurrentFileError = errorMessage,
                            CurrentFileSize = file.Size
                        }, linkedCts!.Token);
                        
                        // Marcar el archivo como fallido
                        if (syncFile != null)
                        {
                            syncFile.MarkAsFailed($"Error crítico: {errorMessage}");
                        }
                        
                        result.FailedCount++;
                        result.DetailedErrors.Add($"{file.Name}: {errorMessage}");
                        result.Success = false;
                        result.ErrorMessage = criticalErrorMessage;
                        
                        _logger.LogWarning("Sincronización pausada automáticamente debido a error crítico: {Error}", errorMessage);
                        
                        // Salir del bucle para que el usuario pueda revisar el problema
                        break;
                    }
                    
                    if (syncFile != null)
                    {
                        syncFile.MarkAsFailed(errorMessage);
                    }
                    
                    result.FailedCount++;
                    result.DetailedErrors.Add($"{file.Name}: {errorMessage}");
                    _logger.LogWarning(ex, "Error procesando archivo {FileName}: {Error}", file.Name, errorMessage);
                }

                await _dbContext.SaveChangesAsync();
                
                // Actualizar progreso
                processedFiles++;
                var percentage = (int)((double)processedFiles / totalFiles * 100);
                await _progressNotifier.NotifyAsync(new SyncProgressUpdate
                {
                    CurrentFile = file.Name,
                    CurrentFileName = file.Name,
                    ProcessedCount = processedFiles,
                    TotalCount = totalFiles,
                    Percentage = percentage,
                    Status = "syncing",
                    Stage = "processing",
                    Message = $"Procesado: {file.Name}",
                    ProcessedFiles = result.ProcessedCount,
                    FailedFiles = result.FailedCount,
                    SkippedFiles = result.AlreadySynced + result.AlreadyExisted,
                    AlreadyExistedFiles = result.AlreadyExisted,
                    CurrentFileStatus = "completed",
                    CurrentFileSize = file.Size
                }, linkedCts!.Token);
            }

            config.RecordSuccessfulSync(result.ProcessedCount);
            result.Success = result.FailedCount == 0;
            
            // Validar integridad: las estadísticas deben sumar el total de archivos soportados
            var totalProcessed = result.ProcessedCount + result.AlreadySynced + result.AlreadyExisted + result.FailedCount;
            if (totalProcessed != totalFiles)
            {
                _logger.LogWarning(
                    "INCONSISTENCIA: Total procesado ({TotalProcessed}) != Total archivos ({TotalFiles}). Diferencia: {Diff}",
                    totalProcessed, totalFiles, totalFiles - totalProcessed);
                result.SkippedCount = totalFiles - totalProcessed;
            }
            
            _logger.LogInformation(
                "Sincronización completada: {Processed} procesados, {AlreadySynced} ya sincronizados, {AlreadyExisted} ya en BD, {Failed} fallidos. Total: {Total}/{Expected}",
                result.ProcessedCount, result.AlreadySynced, result.AlreadyExisted, result.FailedCount, totalProcessed, totalFiles);
                
            // Construir mensaje de resumen
            var summaryParts = new List<string>();
            if (result.ProcessedCount > 0) summaryParts.Add($"{result.ProcessedCount} procesados");
            if (result.AlreadySynced > 0) summaryParts.Add($"{result.AlreadySynced} ya sincronizados");
            if (result.AlreadyExisted > 0) summaryParts.Add($"{result.AlreadyExisted} ya en BD");
            if (result.FailedCount > 0) summaryParts.Add($"{result.FailedCount} fallidos");
            
            var summaryMessage = summaryParts.Count > 0 
                ? $"Sincronización completada: {string.Join(", ", summaryParts)}"
                : "Sincronización completada sin cambios";
                
            // Enviar progreso completado
            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
            {
                CurrentFile = "Completado",
                CurrentFileName = "Completado",
                ProcessedCount = processedFiles,
                TotalCount = totalFiles,
                Percentage = 100,
                Status = "completed",
                Stage = "completed",
                Message = summaryMessage,
                ProcessedFiles = result.ProcessedCount,
                FailedFiles = result.FailedCount,
                SkippedFiles = result.AlreadySynced,
                AlreadyExistedFiles = result.AlreadyExisted
            }, linkedCts!.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Sincronización cancelada");
            result.Success = false;
            result.ErrorMessage = "Sincronización cancelada por el usuario";
            
            // Enviar progreso de cancelación
            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
            {
                CurrentFile = "Cancelado",
                ProcessedCount = 0,
                TotalCount = 0,
                Percentage = 0,
                Status = "cancelled",
                Message = "Sincronización cancelada por el usuario"
            });
        }
        catch (Exception ex)
        {
            config?.RecordFailedSync(ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error durante la sincronización con OneDrive");
            
            // Enviar progreso de error
            await _progressNotifier.NotifyAsync(new SyncProgressUpdate
            {
                CurrentFile = "Error",
                ProcessedCount = 0,
                TotalCount = 0,
                Percentage = 0,
                Status = "error",
                Message = $"Error: {ex.Message}"
            });
        }
        finally
        {
            lock (_syncLock)
            {
                _syncInProgress = false;
                _syncPaused = false;
                _currentSyncCts?.Dispose();
                _currentSyncCts = null;
            }
            
            linkedCts?.Dispose();
        }

        await _dbContext.SaveChangesAsync();
        return result;
    }

    public async Task<List<SyncedFile>> GetSyncedFilesAsync(int page = 1, int pageSize = 50)
    {
        // Devolver todos los archivos sincronizados de todas las fuentes
        // El filtrado por fuente específica se puede hacer en el frontend si es necesario
        return await _dbContext.SyncedFiles
            .OrderByDescending(s => s.SyncedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<OneDriveFolderInfo?> ValidateAndGetFolderInfoAsync(
        string tenantId, string clientId, string clientSecret, string folderPath, string? driveId = null)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(tenantId, clientId, clientSecret);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new OneDriveFolderInfo
                {
                    IsValid = false,
                    ErrorMessage = "No se pudo obtener el token de acceso. Verifica las credenciales de Azure AD."
                };
            }

            var files = await ListFilesInFolderAsync(accessToken, folderPath, driveId);
            
            return new OneDriveFolderInfo
            {
                IsValid = true,
                FolderPath = folderPath,
                FileCount = files.Count,
                InvoiceFileCount = files.Count(f => SupportedExtensions.Contains(Path.GetExtension(f.Name).ToLower()))
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validando carpeta de OneDrive: {Path}", folderPath);
            
            // Extraer mensaje de error más descriptivo
            var errorMessage = ex.Message;
            if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
            {
                errorMessage = "No autorizado. Verifica que la aplicación tenga los permisos 'Files.Read.All' o 'Sites.Read.All' en Azure AD.";
            }
            else if (ex.Message.Contains("404") || ex.Message.Contains("NotFound"))
            {
                errorMessage = $"No se encontró la carpeta '{folderPath}'. Verifica que la ruta sea correcta y que la aplicación tenga acceso a ella.";
            }
            else if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
            {
                errorMessage = "Acceso denegado. Verifica que la aplicación tenga los permisos necesarios y que la carpeta exista.";
            }
            else if (ex.Message.Contains("/me/drive") || ex.Message.Contains("delegated authentication flow"))
            {
                errorMessage = "El endpoint '/me/drive' no funciona con aplicaciones daemon (client_credentials).\n\n" +
                              "Para OneDrive Business, tienes dos opciones:\n" +
                              "1. Usar SharePoint: Proporciona el Drive ID o Site ID de SharePoint en el campo 'Drive ID (opcional)'\n" +
                              "2. Obtener el Drive ID: Puedes obtenerlo desde la URL de SharePoint o usando Microsoft Graph Explorer\n\n" +
                              "El formato del Drive ID es un GUID como: b!abc123...\n" +
                              "O puedes usar el Site ID de SharePoint si tienes acceso al sitio.";
            }
            
            return new OneDriveFolderInfo
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)?> DownloadFileAsync(Guid syncedFileId)
    {
        try
        {
            // Obtener el archivo sincronizado de la base de datos
            var syncedFile = await _dbContext.SyncedFiles
                .FirstOrDefaultAsync(f => f.Id == syncedFileId);

            if (syncedFile == null)
            {
                _logger.LogWarning("Archivo sincronizado no encontrado: {FileId}", syncedFileId);
                return null;
            }

            // Obtener la configuración
            var config = await GetConfigAsync();
            if (config == null || !config.IsConfigured())
            {
                _logger.LogError("OneDrive no está configurado");
                return null;
            }

            // Obtener token de acceso
            var accessToken = await GetAccessTokenAsync(config.TenantId!, config.ClientId!, config.ClientSecret!);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("No se pudo obtener token de acceso para descargar archivo");
                return null;
            }

            // Construir la URL de descarga usando el ID del archivo (ExternalId)
            // En lugar de usar la ruta, usamos el ID directo que es más confiable
            _logger.LogInformation("Configuración descarga - DriveId: '{DriveId}', ExternalId: '{ExternalId}', FileName: '{FileName}'", 
                config.DriveId ?? "null", syncedFile.ExternalId ?? "null", syncedFile.FileName);
            
            // Limpiar y validar el DriveId antes de usarlo
            var cleanDriveId = config.DriveId?.Trim();
            
            string downloadUrl;
            if (string.IsNullOrWhiteSpace(cleanDriveId))
            {
                // Sin Drive ID, usar endpoint de usuario personal
                downloadUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{syncedFile.ExternalId}/content";
                _logger.LogInformation("Usando endpoint personal (sin Drive ID)");
            }
            else
            {
                // Con Drive ID, usar endpoint de SharePoint
                downloadUrl = $"https://graph.microsoft.com/v1.0/drives/{cleanDriveId}/items/{syncedFile.ExternalId}/content";
                _logger.LogInformation("Usando endpoint de SharePoint con Drive ID");
            }

            _logger.LogInformation("URL de descarga: {Url}", downloadUrl);

            // Descargar el archivo
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync(downloadUrl);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error descargando archivo de OneDrive. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }

            // Determinar el content type basado en la extensión
            var extension = Path.GetExtension(syncedFile.FileName).ToLower();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var fileStream = await response.Content.ReadAsStreamAsync();
            return (fileStream, contentType, syncedFile.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descargando archivo: {FileId}", syncedFileId);
            return null;
        }
    }

    /// <summary>
    /// PELIGRO: Limpia completamente la base de datos eliminando todos los registros.
    /// Esta operación es IRREVERSIBLE.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        _logger.LogWarning("¡ADVERTENCIA! Iniciando limpieza completa de la base de datos");

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Cargar todos los registros en memoria primero para evitar problemas de tracking
            _logger.LogInformation("Cargando datos...");
            
            var invoiceLines = await _dbContext.InvoiceReceivedLines.ToListAsync();
            var invoicePayments = await _dbContext.InvoiceReceivedPayments.ToListAsync();
            var invoicesReceived = await _dbContext.InvoicesReceived.ToListAsync();
            var reconciliationCandidates = await _dbContext.ReconciliationCandidates.ToListAsync();
            var bankTransactions = await _dbContext.BankTransactions.ToListAsync();
            var bankAccounts = await _dbContext.BankAccounts.ToListAsync();
            var issuedInvoices = await _dbContext.IssuedInvoices.ToListAsync();
            var invoices = await _dbContext.Invoices.ToListAsync();
            var tasks = await _dbContext.Tasks.ToListAsync();
            var syncedFiles = await _dbContext.SyncedFiles.ToListAsync();

            _logger.LogInformation("Eliminando datos en orden correcto...");

            // 1. Eliminar líneas y pagos de facturas recibidas
            if (invoiceLines.Any())
            {
                _dbContext.InvoiceReceivedLines.RemoveRange(invoiceLines);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminadas {Count} líneas de facturas recibidas", invoiceLines.Count);
            }

            if (invoicePayments.Any())
            {
                _dbContext.InvoiceReceivedPayments.RemoveRange(invoicePayments);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminados {Count} pagos de facturas recibidas", invoicePayments.Count);
            }

            // 2. Eliminar facturas recibidas
            if (invoicesReceived.Any())
            {
                _dbContext.InvoicesReceived.RemoveRange(invoicesReceived);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminadas {Count} facturas recibidas", invoicesReceived.Count);
            }

            // 3. Eliminar candidatos de reconciliación
            if (reconciliationCandidates.Any())
            {
                _dbContext.ReconciliationCandidates.RemoveRange(reconciliationCandidates);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminados {Count} candidatos de reconciliación", reconciliationCandidates.Count);
            }

            // 4. Eliminar transacciones bancarias
            if (bankTransactions.Any())
            {
                _dbContext.BankTransactions.RemoveRange(bankTransactions);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminadas {Count} transacciones bancarias", bankTransactions.Count);
            }

            // 5. Eliminar cuentas bancarias
            if (bankAccounts.Any())
            {
                _dbContext.BankAccounts.RemoveRange(bankAccounts);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminadas {Count} cuentas bancarias", bankAccounts.Count);
            }

            // 6. Eliminar facturas emitidas
            if (issuedInvoices.Any())
            {
                _dbContext.IssuedInvoices.RemoveRange(issuedInvoices);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminadas {Count} facturas emitidas", issuedInvoices.Count);
            }

            // 7. Eliminar facturas (legacy)
            if (invoices.Any())
            {
                _dbContext.Invoices.RemoveRange(invoices);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminadas {Count} facturas legacy", invoices.Count);
            }

            // 8. Eliminar tareas
            if (tasks.Any())
            {
                _dbContext.Tasks.RemoveRange(tasks);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminadas {Count} tareas", tasks.Count);
            }

            // 9. Eliminar archivos sincronizados
            if (syncedFiles.Any())
            {
                _dbContext.SyncedFiles.RemoveRange(syncedFiles);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Eliminados {Count} archivos sincronizados", syncedFiles.Count);
            }

            // Nota: No reseteamos la configuración de OneDrive porque es configuración del usuario
            // y probablemente quiera mantenerla después de limpiar los datos

            await transaction.CommitAsync();
            _logger.LogWarning("¡Base de datos limpiada completamente!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al limpiar la base de datos. Rollback de la transacción.");
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Obtiene un access token, usando cache si aún es válido o refrescando si expiró.
    /// </summary>
    private async Task<string> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret)
    {
        // Guardar credenciales para poder refrescar después
        _currentTenantId = tenantId;
        _currentClientId = clientId;
        _currentClientSecret = clientSecret;
        
        // Si tenemos un token válido (con 5 minutos de margen), usarlo
        if (!string.IsNullOrEmpty(_currentAccessToken) && DateTime.UtcNow < _tokenExpiration.AddMinutes(-5))
        {
            _logger.LogDebug("Usando token cacheado (expira en {Minutes} minutos)", 
                (_tokenExpiration - DateTime.UtcNow).TotalMinutes);
            return _currentAccessToken;
        }
        
        _logger.LogInformation("Obteniendo nuevo access token de Azure AD...");
        var token = await FetchNewAccessTokenAsync(tenantId, clientId, clientSecret);
        
        return token ?? throw new Exception("No se pudo obtener el token de acceso");
    }
    
    /// <summary>
    /// Fuerza el refresh del token (usado cuando se detecta un 401).
    /// </summary>
    private async Task<string> RefreshAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_currentTenantId) || 
            string.IsNullOrEmpty(_currentClientId) || 
            string.IsNullOrEmpty(_currentClientSecret))
        {
            throw new Exception("No hay credenciales guardadas para refrescar el token");
        }
        
        _logger.LogWarning("Token expirado detectado, refrescando...");
        
        // Invalidar token actual
        _currentAccessToken = null;
        _tokenExpiration = DateTime.MinValue;
        
        var token = await FetchNewAccessTokenAsync(_currentTenantId, _currentClientId, _currentClientSecret);
        
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("No se pudo refrescar el token de acceso");
        }
        
        // Actualizar el header de autorización del HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        _logger.LogInformation("Token refrescado correctamente");
        return token;
    }
    
    /// <summary>
    /// Obtiene un nuevo token de Azure AD.
    /// </summary>
    private async Task<string?> FetchNewAccessTokenAsync(string tenantId, string clientId, string clientSecret)
    {
        // Validar que el Client ID no sea un Site ID de SharePoint
        if (clientId.Contains(",") || clientId.Contains(".sharepoint.com"))
        {
            var errorMsg = $"ERROR DE CONFIGURACIÓN: El Client ID parece ser un Site ID de SharePoint en lugar del Application (client) ID de Azure AD.\n\n" +
                          $"Client ID recibido: {clientId}\n\n" +
                          "Configuración correcta:\n" +
                          "- Client ID: Debe ser un GUID simple de tu aplicación en Azure AD (ejemplo: 5feed6b3-3666-419f-bbbf-4f64b0a83ebd)\n" +
                          "- Drive ID (campo opcional): Aquí va el Site ID de SharePoint (ejemplo: moyraonline.sharepoint.com,abc...)\n\n" +
                          "Ve a Azure Portal > Azure Active Directory > App registrations y copia el 'Application (client) ID', no el Site ID.";
            _logger.LogError(errorMsg);
            throw new Exception(errorMsg);
        }
        
        try
        {
            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials"
            });

            var response = await _httpClient.PostAsync(tokenEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error obteniendo token de Azure AD. Status: {Status}, Error: {Error}", response.StatusCode, error);
                
                // Intentar parsear el error para dar un mensaje más descriptivo
                string? errorDescription = null;
                string? errorCode = null;
                try
                {
                    using var errorDoc = JsonDocument.Parse(error);
                    if (errorDoc.RootElement.TryGetProperty("error_description", out var errorDesc))
                    {
                        errorDescription = errorDesc.GetString();
                        _logger.LogError("Descripción del error: {Description}", errorDescription);
                    }
                    if (errorDoc.RootElement.TryGetProperty("error", out var errorObj))
                    {
                        errorCode = errorObj.GetString();
                    }
                }
                catch
                {
                    // Si no se puede parsear, usar el error tal cual
                }
                
                // Detectar errores específicos comunes y lanzar excepciones descriptivas
                if (errorDescription != null)
                {
                    if (errorDescription.Contains("AADSTS7000215") || errorDescription.Contains("Invalid client secret"))
                    {
                        var message = "Client Secret inválido. Estás usando el Client Secret ID en lugar del Client Secret Value.\n\n" +
                                     "En Azure AD, cuando creas un secret obtienes:\n" +
                                     "- Secret ID: Un GUID (ejemplo: 12345678-1234-1234-1234-123456789012)\n" +
                                     "- Secret Value: El valor real del secreto (solo se muestra una vez al crearlo)\n\n" +
                                     "Debes usar el Secret VALUE, no el ID. Si perdiste el Secret Value, necesitas crear uno nuevo en Azure AD.";
                        _logger.LogError("ERROR: {Message}", message);
                        throw new Exception(message);
                    }
                    else if (errorDescription.Contains("AADSTS700016") || errorDescription.Contains("Application was not found"))
                    {
                        var message = "La aplicación no se encontró en Azure AD. Verifica que el Client ID sea correcto.";
                        _logger.LogError("ERROR: {Message}", message);
                        throw new Exception(message);
                    }
                    else if (errorDescription.Contains("AADSTS50034") || errorDescription.Contains("User account"))
                    {
                        var message = "Problema con el Tenant ID. Verifica que el Tenant ID (Directory ID) sea correcto.";
                        _logger.LogError("ERROR: {Message}", message);
                        throw new Exception(message);
                    }
                }
                
                // Si no es un error conocido, lanzar excepción genérica con el error de Azure AD
                throw new Exception($"Error de autenticación con Azure AD: {errorDescription ?? error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            
            // Guardar el token y calcular expiración (típicamente 1 hora, pero leemos expires_in)
            if (!string.IsNullOrEmpty(accessToken))
            {
                _currentAccessToken = accessToken;
                
                // Leer expires_in (en segundos), default 3600 (1 hora)
                var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expProp) 
                    ? expProp.GetInt32() 
                    : 3600;
                    
                _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn);
                _logger.LogInformation("Token obtenido. Expira en {Minutes} minutos", expiresIn / 60);
            }
            
            return accessToken;
        }
        catch (System.Net.Http.HttpRequestException httpEx) when (httpEx.InnerException is System.Net.Sockets.SocketException)
        {
            var socketEx = httpEx.InnerException as System.Net.Sockets.SocketException;
            var errorMsg = socketEx?.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound
                ? "No se puede conectar a Azure AD. Verifica tu conexión a internet y que el DNS esté funcionando correctamente."
                : $"Error de red al conectar con Azure AD: {socketEx?.Message ?? httpEx.Message}";
            
            _logger.LogError(httpEx, "Error de conectividad al obtener token de Azure AD: {Error}", errorMsg);
            throw new Exception(errorMsg, httpEx);
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            var errorMsg = socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound
                ? "No se puede conectar a Azure AD. Verifica tu conexión a internet y que el DNS esté funcionando correctamente."
                : $"Error de red: {socketEx.Message}";
            
            _logger.LogError(socketEx, "Error de socket al obtener token de Azure AD: {Error}", errorMsg);
            throw new Exception(errorMsg, socketEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener token de Azure AD");
            throw new Exception($"Error al obtener token de Azure AD: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Ejecuta una petición a Microsoft Graph con auto-retry y token refresh en caso de 401.
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteGraphRequestWithRetryAsync(
        Func<(string driveUrl, string siteUrl)> urlBuilder, 
        string context,
        int maxRetries = 2)
    {
        var (driveUrl, siteUrl) = urlBuilder();
        HttpResponseMessage? response = null;
        var currentUrl = driveUrl;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            // Intentar con Drive ID primero
            _logger.LogDebug("Intento {Attempt}: Explorando {Context} con URL: {Url}", attempt + 1, context, currentUrl);
            response = await _httpClient.GetAsync(currentUrl);
            
            // Si es 401, intentar refrescar el token
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Verificar si es un error de token expirado
                if (errorContent.Contains("InvalidAuthenticationToken") || 
                    errorContent.Contains("token") ||
                    errorContent.Contains("expired") ||
                    errorContent.Contains("Access token has expired"))
                {
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning("Token expirado detectado (401) en intento {Attempt}. Refrescando token...", attempt + 1);
                        
                        try
                        {
                            await RefreshAccessTokenAsync();
                            // Continuar al siguiente intento con el nuevo token
                            continue;
                        }
                        catch (Exception refreshEx)
                        {
                            _logger.LogError(refreshEx, "Error al refrescar token");
                            throw new Exception($"Token expirado y no se pudo refrescar: {refreshEx.Message}", refreshEx);
                        }
                    }
                }
            }
            
            // Si el Drive ID no funciona, intentar como Site ID
            if (!response.IsSuccessStatusCode && currentUrl == driveUrl && !string.IsNullOrEmpty(siteUrl))
            {
                _logger.LogInformation("Primer intento falló (Status: {Status}), intentando como Site ID directo", response.StatusCode);
                currentUrl = siteUrl;
                response = await _httpClient.GetAsync(currentUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Site ID directo funcionó correctamente para: {Context}", context);
                    break;
                }
            }
            else if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Acceso exitoso a: {Context}", context);
                break;
            }
            
            // Si llegamos aquí con un error no-401, no reintentar
            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                break;
            }
        }
        
        return response!;
    }
    
    /// <summary>
    /// Construye las URLs para listar archivos en una carpeta.
    /// </summary>
    private (string driveUrl, string siteUrl) BuildFolderListUrl(string driveId, string folderPath)
    {
        var driveUrl = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:/{folderPath}:/children";
        var siteUrl = $"https://graph.microsoft.com/v1.0/sites/{driveId}/drive/root:/{folderPath}:/children";
        return (driveUrl, siteUrl);
    }
    
    /// <summary>
    /// Maneja los errores de Microsoft Graph con mensajes descriptivos.
    /// </summary>
    private async Task HandleGraphErrorAsync(HttpResponseMessage response, string folderPath, string driveId)
    {
        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Error de Microsoft Graph. Status: {Status}, Error: {Error}", response.StatusCode, error);
        
        // Intentar extraer mensaje de error más descriptivo
        string errorMessage = $"Error accediendo a OneDrive: {response.StatusCode}";
        string? graphErrorCode = null;
        string? graphErrorMessage = null;
        
        try
        {
            using var errorDoc = JsonDocument.Parse(error);
            if (errorDoc.RootElement.TryGetProperty("error", out var errorObj))
            {
                if (errorObj.TryGetProperty("message", out var message))
                {
                    graphErrorMessage = message.GetString();
                    errorMessage = $"Error de Microsoft Graph: {graphErrorMessage}";
                }
                if (errorObj.TryGetProperty("code", out var code))
                {
                    graphErrorCode = code.GetString();
                }
            }
        }
        catch
        {
            // Si no se puede parsear, usar el mensaje genérico
        }
        
        // Manejar errores específicos
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            errorMessage = "Error de autenticación (401). El token ha expirado y no se pudo refrescar.\n\n" +
                          "Posibles causas:\n" +
                          "1. El Client Secret ha expirado en Azure AD\n" +
                          "2. Los permisos de la aplicación fueron revocados\n" +
                          "3. Problema de conectividad con Azure AD\n\n" +
                          "Verifica la configuración en Azure Portal.\n\n" +
                          $"Error técnico: {graphErrorMessage ?? error}";
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || graphErrorCode == "accessDenied")
        {
            errorMessage = "Acceso denegado. Verifica los permisos de aplicación en Azure AD.\n\n" +
                          $"Error técnico: {graphErrorMessage ?? error}";
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            errorMessage = $"No se encontró la carpeta '{folderPath}'. Verifica que la ruta sea correcta.\n\n" +
                          $"Error técnico: {graphErrorMessage ?? error}";
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var isInvalidDriveId = error.Contains("malformed") || 
                                   error.Contains("invalidRequest") ||
                                   error.Contains("does not represent a valid drive");
            if (isInvalidDriveId)
            {
                errorMessage = $"El ID proporcionado no es válido: {driveId}\n\n" +
                              "Verifica el formato del Drive ID o Site ID.\n\n" +
                              $"Error técnico: {graphErrorMessage ?? error}";
            }
        }
        
        throw new Exception(errorMessage);
    }

    private async Task<List<OneDriveFile>> ListFilesInFolderAsync(string accessToken, string folderPath, string? driveId)
    {
        var files = new List<OneDriveFile>();
        
        // Limpiar headers anteriores y establecer el nuevo token
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        // Sin Drive ID ni Site ID, no podemos usar /me/drive con client_credentials
        if (string.IsNullOrWhiteSpace(driveId))
        {
            _logger.LogError("ListFilesInFolderAsync llamado sin Drive ID ni Site ID. Esto no es compatible con client_credentials.");
            throw new Exception(
                "Se requiere un Drive ID o Site ID para aplicaciones daemon (client_credentials).\n\n" +
                "Para SharePoint/OneDrive Business:\n" +
                "1. Obtén el Drive ID desde SharePoint o Microsoft Graph Explorer\n" +
                "2. O usa el Site ID de SharePoint\n" +
                "3. Ingresa el ID en el campo 'Drive ID (opcional)'\n\n" +
                "El endpoint '/me/drive' solo funciona con autenticación delegada (usuario), no con aplicaciones daemon.");
        }
        
        // Validar que el driveId no sea "me" (que causaría el error)
        if (driveId.Trim().Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Drive ID no puede ser 'me' para aplicaciones daemon");
            throw new Exception(
                "El Drive ID no puede ser 'me' para aplicaciones daemon (client_credentials).\n\n" +
                "Debes proporcionar un Site ID o Drive ID válido de SharePoint.\n\n" +
                "El endpoint '/me/drive' solo funciona con autenticación delegada (usuario), no con aplicaciones daemon.");
        }

        // Intentar primero obtener el Drive ID desde el Site ID (más común en SharePoint)
        // Esto funciona mejor porque primero obtenemos el Drive ID y luego lo usamos
        string? actualDriveId = null;
        
        // Intentar obtener el Drive ID desde el Site ID primero
        _logger.LogInformation("Intentando obtener Drive ID desde Site ID. DriveId recibido: '{DriveId}', FolderPath: '{FolderPath}'", driveId, folderPath);
        actualDriveId = await GetDriveIdFromSiteAsync(accessToken, driveId);
        if (!string.IsNullOrEmpty(actualDriveId))
        {
            _logger.LogInformation("Drive ID obtenido desde Site: {DriveId}", actualDriveId);
        }
        else
        {
            // Si no se pudo obtener como Site ID, usar el driveId directamente
            _logger.LogInformation("No se pudo obtener Drive ID desde Site ID, usando DriveId proporcionado: {DriveId}", driveId);
            actualDriveId = driveId;
        }
        
        // Llamar al método recursivo para obtener todos los archivos
        await ListFilesRecursivelyAsync(accessToken, folderPath, actualDriveId, files);
        
        _logger.LogInformation("Búsqueda recursiva completada. Total de archivos encontrados: {Count}", files.Count);
        return files;
    }

    /// <summary>
    /// Lista archivos recursivamente en una carpeta y todas sus subcarpetas.
    /// Incluye auto-refresh del token cuando expira (401).
    /// </summary>
    private async Task ListFilesRecursivelyAsync(string accessToken, string folderPath, string driveId, List<OneDriveFile> files)
    {
        var response = await ExecuteGraphRequestWithRetryAsync(
            () => BuildFolderListUrl(driveId, folderPath),
            folderPath);
        
        if (!response.IsSuccessStatusCode)
        {
            await HandleGraphErrorAsync(response, folderPath, driveId);
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        
        var subfolders = new List<string>();
        
        if (doc.RootElement.TryGetProperty("value", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                // Si es una carpeta, agregarla a la lista de subcarpetas para explorar recursivamente
                if (item.TryGetProperty("folder", out _))
                {
                    var folderName = item.GetProperty("name").GetString() ?? "";
                    var subfolderPath = folderPath + "/" + folderName;
                    subfolders.Add(subfolderPath);
                    _logger.LogInformation("Subcarpeta encontrada: {SubfolderPath}", subfolderPath);
                    continue;
                }
                
                // Solo procesar archivos (no carpetas)
                if (!item.TryGetProperty("file", out _))
                    continue;

                var fileName = item.GetProperty("name").GetString() ?? "";
                var extension = Path.GetExtension(fileName).ToLower();
                
                // Solo archivos soportados
                if (!SupportedExtensions.Contains(extension))
                    continue;

                var file = new OneDriveFile
                {
                    Id = item.GetProperty("id").GetString() ?? "",
                    Name = fileName,
                    Path = folderPath + "/" + fileName,
                    Size = item.GetProperty("size").GetInt64(),
                    LastModified = item.GetProperty("lastModifiedDateTime").GetDateTime(),
                    Hash = item.TryGetProperty("file", out var fileInfo) && 
                           fileInfo.TryGetProperty("hashes", out var hashes) &&
                           hashes.TryGetProperty("quickXorHash", out var hash)
                        ? hash.GetString() ?? ""
                        : ComputeSimpleHash(item.GetProperty("id").GetString() + item.GetProperty("size").GetInt64()),
                    DownloadUrl = item.TryGetProperty("@microsoft.graph.downloadUrl", out var downloadUrl)
                        ? downloadUrl.GetString() ?? ""
                        : ""
                };

                files.Add(file);
                _logger.LogDebug("Archivo encontrado: {FileName} en {FolderPath}", fileName, folderPath);
            }
        }

        // Explorar recursivamente todas las subcarpetas encontradas
        foreach (var subfolder in subfolders)
        {
            try
            {
                await ListFilesRecursivelyAsync(accessToken, subfolder, driveId, files);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error explorando subcarpeta: {Subfolder}. Se continuará con otras carpetas.", subfolder);
                // Continuar con las demás carpetas aunque una falle
            }
        }
    }

    private async Task<byte[]> DownloadFileAsync(string accessToken, string downloadUrl, string? fileId = null, string? driveId = null)
    {
        if (string.IsNullOrEmpty(downloadUrl))
            throw new Exception("URL de descarga no disponible");

        // El downloadUrl ya incluye el token temporal de Microsoft
        // Sin embargo, estos tokens también pueden expirar (~1 hora)
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        
        // Crear un CancellationToken con timeout para la descarga inicial
        using var initialDownloadCts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var response = await _httpClient.GetAsync(downloadUrl, initialDownloadCts.Token);
        
        // Si el download URL expiró (401), intentar obtener una nueva URL
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Download URL expirado (401). Intentando obtener nueva URL de descarga...");
            
            // Si tenemos fileId, podemos obtener una nueva URL usando el endpoint directo de Graph
            if (!string.IsNullOrEmpty(fileId))
            {
                try
                {
                    // Intentar refrescar el token primero
                    string refreshedToken;
                    try
                    {
                        refreshedToken = await RefreshAccessTokenAsync();
                    }
                    catch (Exception tokenEx)
                    {
                        _logger.LogWarning(tokenEx, "No se pudo refrescar token desde variables de instancia. Intentando obtener nuevo token desde configuración...");
                        // Si no podemos refrescar desde variables de instancia, obtener token desde configuración
                        var config = await GetConfigAsync();
                        if (config == null || !config.IsConfigured())
                        {
                            throw new Exception("No hay configuración disponible para refrescar el token");
                        }
                        refreshedToken = await GetAccessTokenAsync(config.TenantId!, config.ClientId!, config.ClientSecret!);
                        if (string.IsNullOrEmpty(refreshedToken))
                        {
                            throw new Exception("No se pudo obtener un nuevo token de acceso");
                        }
                    }
                    
                    // Obtener una nueva URL de descarga usando el endpoint directo de Graph
                    // En lugar de usar downloadUrl, usamos el endpoint /content directamente
                    string directDownloadUrl;
                    if (string.IsNullOrWhiteSpace(driveId))
                    {
                        directDownloadUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{fileId}/content";
                    }
                    else
                    {
                        directDownloadUrl = $"https://graph.microsoft.com/v1.0/drives/{driveId.Trim()}/items/{fileId}/content";
                    }
                    
                    _logger.LogInformation("Usando endpoint directo de Graph para descargar archivo: {Url}", directDownloadUrl);
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {refreshedToken}");
                    
                    // Crear un CancellationToken con timeout más largo para la descarga
                    using var downloadCts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
                    response = await _httpClient.GetAsync(directDownloadUrl, downloadCts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Archivo descargado exitosamente usando endpoint directo");
                        return await response.Content.ReadAsByteArrayAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Endpoint directo también falló. Status: {Status}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener nueva URL de descarga después de expiración");
                    
                    // Si el error es de token, lanzar excepción específica
                    if (IsTokenError(ex))
                    {
                        throw new Exception($"Error de token al obtener nueva URL de descarga: {ex.Message}", ex);
                    }
                }
            }
            
            // Si no pudimos obtener una nueva URL o no tenemos fileId, lanzar excepción
            throw new Exception("La URL de descarga ha expirado y no se pudo obtener una nueva. El archivo será procesado en la próxima sincronización.");
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>
    /// Obtiene una nueva URL de descarga para un archivo específico usando la API de Graph.
    /// </summary>
    private async Task<string?> GetFileDownloadUrlAsync(string accessToken, string fileId, string? driveId = null)
    {
        try
        {
            string apiUrl;
            if (string.IsNullOrWhiteSpace(driveId))
            {
                // Sin Drive ID, usar endpoint de usuario personal
                apiUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{fileId}";
            }
            else
            {
                // Con Drive ID, usar endpoint de SharePoint
                apiUrl = $"https://graph.microsoft.com/v1.0/drives/{driveId.Trim()}/items/{fileId}";
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            // Timeout más corto para obtener metadatos (no necesita descargar el archivo completo)
            using var metadataCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.GetAsync(apiUrl, metadataCts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error obteniendo información del archivo. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            // Buscar la propiedad @microsoft.graph.downloadUrl
            if (doc.RootElement.TryGetProperty("@microsoft.graph.downloadUrl", out var downloadUrlElement))
            {
                var downloadUrl = downloadUrlElement.GetString();
                _logger.LogInformation("Nueva URL de descarga obtenida exitosamente");
                return downloadUrl;
            }
            
            _logger.LogWarning("No se encontró @microsoft.graph.downloadUrl en la respuesta");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener nueva URL de descarga para archivo {FileId}", fileId);
            return null;
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Obtiene el Drive ID desde un Site ID de SharePoint
    /// </summary>
    private async Task<string?> GetDriveIdFromSiteAsync(string accessToken, string siteId)
    {
        try
        {
            var url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drive";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("id", out var driveId))
                {
                    var driveIdValue = driveId.GetString();
                    _logger.LogInformation("Drive ID obtenido desde Site ID: {DriveId}", driveIdValue);
                    return driveIdValue;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error obteniendo Drive desde Site ID {SiteId}: {Error}", siteId, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Excepción al obtener Drive desde Site ID: {SiteId}", siteId);
        }
        
        return null;
    }

    /// <summary>
    /// Detecta si una excepción está relacionada con problemas de token/autenticación, rate limiting, o URLs expiradas.
    /// </summary>
    private static bool IsTokenError(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";
        
        // Patrones que indican error de token
        var tokenErrorPatterns = new[]
        {
            "401",
            "unauthorized",
            "invalidauthenticationtoken",
            "token expired",
            "token has expired",
            "access token has expired",
            "invalid client secret",
            "aadsts7000215",
            "aadsts7000222",
            "aadsts7000234",
            "error de token",
            "no se pudo obtener el token",
            "no se pudo refrescar",
            "token expirado",
            "authentication failed",
            "invalid token"
        };
        
        return tokenErrorPatterns.Any(pattern => 
            message.Contains(pattern) || 
            innerMessage.Contains(pattern));
    }

    /// <summary>
    /// Detecta si un error es crítico y requiere pausar la sincronización automáticamente.
    /// Incluye: URLs expiradas que no se pueden refrescar, rate limiting, permisos, timeouts críticos.
    /// </summary>
    private static bool IsCriticalError(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";
        
        // Patrones que indican error crítico que requiere pausa
        var criticalErrorPatterns = new[]
        {
            // URLs expiradas que no se pueden refrescar
            "url de descarga ha expirado y no se pudo obtener una nueva",
            "no se pudo obtener una nueva",
            "download url expirado",
            
            // Rate limiting de Microsoft Graph
            "429",
            "too many requests",
            "rate limit",
            "throttled",
            "quota exceeded",
            
            // Errores de permisos
            "403",
            "forbidden",
            "access denied",
            "insufficient privileges",
            "does not have permission",
            
            // Timeouts críticos (solo si es un timeout después de múltiples intentos)
            "timeout",
            "operation timed out",
            "request timeout",
            
            // Errores de configuración
            "no hay configuración disponible",
            "no se pudo obtener un nuevo token",
            "client secret",
            "invalid client",
            
            // Errores de token que no se pueden resolver
            "error de token al obtener nueva url",
            "no se pudo refrescar el token"
        };
        
        return criticalErrorPatterns.Any(pattern => 
            message.Contains(pattern) || 
            innerMessage.Contains(pattern));
    }

    private static string ComputeSimpleHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

public class OneDriveSyncConfigDto
{
    public bool Enabled { get; set; }
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string? DriveId { get; set; }
    public int SyncIntervalMinutes { get; set; } = 15;
}

public class SyncResult
{
    public bool Success { get; set; }
    public int ProcessedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalDetected { get; set; }
    public int AlreadyExisted { get; set; }
    public int AlreadySynced { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> DetailedErrors { get; set; } = new();
    public Dictionary<string, int> FilesByType { get; set; } = new();
}

public class SyncPreview
{
    public int TotalFiles { get; set; }
    public int SupportedFiles { get; set; }
    public int UnsupportedFiles { get; set; }
    public int AlreadySynced { get; set; }
    public int AlreadyExistsInDb { get; set; }
    public int PendingToProcess { get; set; }
    public Dictionary<string, int> FilesByExtension { get; set; } = new();
    public List<string> UnsupportedExtensions { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class OneDriveFile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string Hash { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
}

public class OneDriveFolderInfo
{
    public bool IsValid { get; set; }
    public string? FolderPath { get; set; }
    public int FileCount { get; set; }
    public int InvoiceFileCount { get; set; }
    public string? ErrorMessage { get; set; }
}


