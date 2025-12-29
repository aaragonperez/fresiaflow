using FresiaFlow.Adapters.Outbound.OneDrive;
using FresiaFlow.Domain.Sync;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

[ApiController]
[Route("api/sync/onedrive")]
public class OneDriveSyncController : ControllerBase
{
    private readonly IOneDriveSyncService _syncService;
    private readonly ILogger<OneDriveSyncController> _logger;

    public OneDriveSyncController(
        IOneDriveSyncService syncService,
        ILogger<OneDriveSyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la configuración actual de sincronización de OneDrive
    /// </summary>
    [HttpGet("config")]
    public async Task<ActionResult<OneDriveSyncConfigResponse>> GetConfig()
    {
        try
        {
            var config = await _syncService.GetConfigAsync();
            
            if (config == null)
            {
                return Ok(new OneDriveSyncConfigResponse
                {
                    Configured = false,
                    Enabled = false
                });
            }

            return Ok(new OneDriveSyncConfigResponse
            {
                Configured = config.IsConfigured(),
                Enabled = config.Enabled,
                TenantId = config.TenantId,
                ClientId = config.ClientId,
                // No devolvemos el ClientSecret por seguridad
                HasClientSecret = !string.IsNullOrEmpty(config.ClientSecret),
                FolderPath = config.FolderPath,
                DriveId = config.DriveId,
                SyncIntervalMinutes = config.SyncIntervalMinutes,
                LastSyncAt = config.LastSyncAt,
                LastSyncError = config.LastSyncError,
                TotalFilesSynced = config.TotalFilesSynced
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo configuración de OneDrive");
            return StatusCode(500, new { error = "Error al obtener la configuración de OneDrive", message = ex.Message });
        }
    }

    /// <summary>
    /// Guarda la configuración de sincronización de OneDrive
    /// </summary>
    [HttpPost("config")]
    public async Task<ActionResult<OneDriveSyncConfigResponse>> SaveConfig([FromBody] OneDriveSyncConfigDto dto)
    {
        try
        {
            var config = await _syncService.SaveConfigAsync(dto);

            return Ok(new OneDriveSyncConfigResponse
            {
                Configured = config.IsConfigured(),
                Enabled = config.Enabled,
                TenantId = config.TenantId,
                ClientId = config.ClientId,
                HasClientSecret = !string.IsNullOrEmpty(config.ClientSecret),
                FolderPath = config.FolderPath,
                DriveId = config.DriveId,
                SyncIntervalMinutes = config.SyncIntervalMinutes,
                LastSyncAt = config.LastSyncAt,
                LastSyncError = config.LastSyncError,
                TotalFilesSynced = config.TotalFilesSynced
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando configuración de OneDrive");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Valida la configuración y obtiene información de la carpeta
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<OneDriveFolderInfo>> ValidateConfig([FromBody] ValidateConfigRequest request)
    {
        var result = await _syncService.ValidateAndGetFolderInfoAsync(
            request.TenantId,
            request.ClientId,
            request.ClientSecret,
            request.FolderPath,
            request.DriveId
        );

        if (result == null)
        {
            return BadRequest(new OneDriveFolderInfo
            {
                IsValid = false,
                ErrorMessage = "No se pudo conectar con OneDrive. Verifica la configuración."
            });
        }

        if (!result.IsValid)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un preview de los archivos a sincronizar antes de ejecutar la sincronización
    /// </summary>
    [HttpGet("sync/preview")]
    public async Task<ActionResult<SyncPreview>> GetSyncPreview(CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _syncService.GetSyncPreviewAsync(cancellationToken);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo preview de sincronización");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ejecuta una sincronización manual
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<SyncResult>> SyncNow([FromBody] SyncNowRequest? request = null, CancellationToken cancellationToken = default)
    {
        var forceReprocess = request?.ForceReprocess ?? false;
        _logger.LogInformation("Iniciando sincronización manual de OneDrive (Forzar reproceso: {ForceReprocess})", forceReprocess);
        var result = await _syncService.SyncNowAsync(forceReprocess, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancela la sincronización manual en progreso
    /// </summary>
    [HttpPost("sync/cancel")]
    public IActionResult CancelSync()
    {
        if (!_syncService.IsSyncInProgress)
        {
            return BadRequest(new { error = "No hay ninguna sincronización en progreso" });
        }

        _logger.LogInformation("Cancelando sincronización manual por solicitud del usuario");
        _syncService.CancelCurrentSync();
        return Ok(new { message = "Sincronización cancelada" });
    }

    /// <summary>
    /// Pausa la sincronización manual en progreso
    /// </summary>
    [HttpPost("sync/pause")]
    public IActionResult PauseSync()
    {
        if (!_syncService.IsSyncInProgress)
        {
            return BadRequest(new { error = "No hay ninguna sincronización en progreso" });
        }

        if (_syncService.IsSyncPaused)
        {
            return BadRequest(new { error = "La sincronización ya está pausada" });
        }

        _logger.LogInformation("Pausando sincronización manual por solicitud del usuario");
        _syncService.PauseCurrentSync();
        return Ok(new { message = "Sincronización pausada" });
    }

    /// <summary>
    /// Reanuda la sincronización manual pausada
    /// </summary>
    [HttpPost("sync/resume")]
    public IActionResult ResumeSync()
    {
        if (!_syncService.IsSyncInProgress)
        {
            return BadRequest(new { error = "No hay ninguna sincronización en progreso" });
        }

        if (!_syncService.IsSyncPaused)
        {
            return BadRequest(new { error = "La sincronización no está pausada" });
        }

        _logger.LogInformation("Reanudando sincronización manual por solicitud del usuario");
        _syncService.ResumeCurrentSync();
        return Ok(new { message = "Sincronización reanudada" });
    }

    /// <summary>
    /// Obtiene el estado actual de la sincronización
    /// </summary>
    [HttpGet("sync/status")]
    public IActionResult GetSyncStatus()
    {
        return Ok(new { 
            isSyncing = _syncService.IsSyncInProgress,
            isPaused = _syncService.IsSyncPaused
        });
    }

    /// <summary>
    /// Obtiene el historial de archivos sincronizados
    /// </summary>
    [HttpGet("files")]
    public async Task<ActionResult<List<SyncedFileResponse>>> GetSyncedFiles([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var files = await _syncService.GetSyncedFilesAsync(page, pageSize);
        
        var response = files.Select(f => new SyncedFileResponse
        {
            Id = f.Id,
            FileName = f.FileName,
            FilePath = f.FilePath,
            FileSize = f.FileSize,
            SyncedAt = f.SyncedAt,
            Status = f.Status.ToString(),
            ErrorMessage = f.ErrorMessage,
            InvoiceId = f.InvoiceId,
            Source = f.Source // Incluir el campo Source para identificar el tipo de fuente
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Descarga un archivo sincronizado desde OneDrive
    /// </summary>
    [HttpGet("files/{fileId}/download")]
    public async Task<IActionResult> DownloadFile(Guid fileId)
    {
        try
        {
            var result = await _syncService.DownloadFileAsync(fileId);
            
            if (result == null)
            {
                return NotFound(new { error = "Archivo no encontrado o no disponible" });
            }

            var (fileStream, contentType, fileName) = result.Value;
            
            return File(fileStream, contentType, fileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descargando archivo: {FileId}", fileId);
            return StatusCode(500, new { error = "Error al descargar el archivo", message = ex.Message });
        }
    }

    /// <summary>
    /// PELIGRO: Limpia completamente la base de datos eliminando todos los registros de todas las tablas.
    /// Esta acción es irreversible.
    /// </summary>
    [HttpDelete("clear-database")]
    public async Task<IActionResult> ClearDatabase([FromBody] ClearDatabaseRequest request)
    {
        if (!request.Confirmed || request.ConfirmationCode != "DELETE_ALL_DATA")
        {
            return BadRequest(new { error = "Se requiere confirmación explícita con el código 'DELETE_ALL_DATA'" });
        }

        try
        {
            _logger.LogWarning("¡ALERTA! Iniciando limpieza completa de la base de datos");
            await _syncService.ClearAllDataAsync();
            _logger.LogWarning("Base de datos limpiada completamente");
            return Ok(new { message = "Base de datos limpiada completamente", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al limpiar la base de datos");
            return StatusCode(500, new { error = "Error al limpiar la base de datos", message = ex.Message });
        }
    }
}

public class OneDriveSyncConfigResponse
{
    public bool Configured { get; set; }
    public bool Enabled { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public bool HasClientSecret { get; set; }
    public string? FolderPath { get; set; }
    public string? DriveId { get; set; }
    public int SyncIntervalMinutes { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
    public int TotalFilesSynced { get; set; }
}

public class ValidateConfigRequest
{
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string? DriveId { get; set; }
}

public class SyncedFileResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime SyncedAt { get; set; }
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public Guid? InvoiceId { get; set; }
    public string Source { get; set; } = ""; // "OneDrive-{id}", "Email-{id}", "Portal-{id}", etc.
}

public class SyncNowRequest
{
    public bool ForceReprocess { get; set; }
}

public class ClearDatabaseRequest
{
    public bool Confirmed { get; set; }
    public string ConfirmationCode { get; set; } = "";
}

