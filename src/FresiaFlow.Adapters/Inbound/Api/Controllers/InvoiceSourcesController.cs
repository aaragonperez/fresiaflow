using FresiaFlow.Adapters.Outbound.InvoiceSources;
using FresiaFlow.Adapters.Outbound.Persistence;
using FresiaFlow.Domain.Sync;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FresiaFlow.Adapters.Inbound.Api.Controllers;

/// <summary>
/// Controlador para gestionar fuentes de descarga automática de facturas.
/// </summary>
[ApiController]
[Route("api/invoice-sources")]
public class InvoiceSourcesController : ControllerBase
{
    private readonly FresiaFlowDbContext _dbContext;
    private readonly InvoiceSourceSyncServiceFactory _serviceFactory;
    private readonly SyncInvoicesFromSourcesUseCase _syncUseCase;
    private readonly ILogger<InvoiceSourcesController> _logger;

    public InvoiceSourcesController(
        FresiaFlowDbContext dbContext,
        InvoiceSourceSyncServiceFactory serviceFactory,
        SyncInvoicesFromSourcesUseCase syncUseCase,
        ILogger<InvoiceSourcesController> logger)
    {
        _dbContext = dbContext;
        _serviceFactory = serviceFactory;
        _syncUseCase = syncUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas las fuentes configuradas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var sources = await _dbContext.InvoiceSourceConfigs
            .OrderBy(s => s.SourceType)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return Ok(sources.Select(s => new
        {
            s.Id,
            s.SourceType,
            s.Name,
            s.Enabled,
            s.LastSyncAt,
            s.LastSyncError,
            s.TotalFilesSynced,
            s.CreatedAt,
            s.UpdatedAt
        }));
    }

    /// <summary>
    /// Obtiene una fuente específica.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var source = await _dbContext.InvoiceSourceConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (source == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            source.Id,
            source.SourceType,
            source.Name,
            source.ConfigJson,
            source.Enabled,
            source.LastSyncAt,
            source.LastSyncError,
            source.TotalFilesSynced,
            source.CreatedAt,
            source.UpdatedAt
        });
    }

    /// <summary>
    /// Crea o actualiza una fuente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateOrUpdateSourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            InvoiceSourceConfig config;

            if (request.Id.HasValue)
            {
                // Actualizar existente
                config = await _dbContext.InvoiceSourceConfigs.FindAsync(new object[] { request.Id.Value }, cancellationToken);
                if (config == null)
                {
                    return NotFound();
                }

                // Para OneDrive, normalizar el JSON para preservar syncIntervalMinutes
                string normalizedConfigJson = request.ConfigJson;
                if (config.SourceType == InvoiceSourceType.OneDrive)
                {
                    _logger.LogInformation("🔄 Normalizando JSON de OneDrive para fuente {SourceId} ({SourceName})", 
                        config.Id, config.Name);
                    normalizedConfigJson = NormalizeOneDriveConfigJson(config.ConfigJson, request.ConfigJson);
                    _logger.LogInformation("✅ JSON normalizado. Longitud: {Length} caracteres", normalizedConfigJson.Length);
                }

                config.UpdateConfig(request.Name, normalizedConfigJson);
                if (request.Enabled.HasValue)
                {
                    if (request.Enabled.Value)
                        config.Enable();
                    else
                        config.Disable();
                }
            }
            else
            {
                // Crear nuevo
                config = InvoiceSourceConfig.Create(
                    request.SourceType,
                    request.Name,
                    request.ConfigJson);

                if (request.Enabled == true)
                {
                    config.Enable();
                }

                _dbContext.InvoiceSourceConfigs.Add(config);
            }

            // Validar configuración según el tipo (no bloquea el guardado, solo advierte)
            // La validación puede fallar si el servidor no está accesible, pero permitimos guardar igual
            try
            {
                var validationResult = await ValidateSourceConfigAsync(config, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Configuración guardada pero validación falló: {Error}", validationResult.ErrorMessage);
                    // No bloqueamos el guardado, solo registramos la advertencia
                }
            }
            catch (Exception validationEx)
            {
                _logger.LogWarning(validationEx, "Error durante validación, pero se permite guardar la configuración");
                // Continuamos con el guardado aunque la validación falle
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                config.Id,
                config.SourceType,
                config.Name,
                config.Enabled
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error de base de datos al crear/actualizar fuente");
            // Verificar si es porque la tabla no existe
            if (dbEx.InnerException?.Message?.Contains("does not exist") == true || 
                dbEx.InnerException?.Message?.Contains("no existe") == true)
            {
                return StatusCode(500, new { error = "La tabla InvoiceSourceConfigs no existe. Ejecuta las migraciones de base de datos." });
            }
            var errorMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            _logger.LogError(dbEx, "Error de base de datos: {ErrorMessage}", errorMessage);
            return StatusCode(500, new { error = $"Error de base de datos: {errorMessage}" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Error de validación al crear/actualizar fuente: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error de operación al crear/actualizar fuente: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado creando/actualizando fuente: {Message}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { error = $"Error al guardar la fuente: {ex.Message}" });
        }
    }

    /// <summary>
    /// Elimina una fuente.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var source = await _dbContext.InvoiceSourceConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (source == null)
        {
            return NotFound();
        }

        _dbContext.InvoiceSourceConfigs.Remove(source);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Sincroniza manualmente una fuente específica.
    /// </summary>
    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(Guid id, CancellationToken cancellationToken, [FromQuery] bool forceReprocess = false)
    {
        var source = await _dbContext.InvoiceSourceConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (source == null)
        {
            return NotFound();
        }

        try
        {
            var service = _serviceFactory.GetService(source.SourceType);
            var result = await service.SyncNowAsync(id, forceReprocess, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando fuente {SourceId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene un preview de las facturas disponibles en una fuente.
    /// </summary>
    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> GetPreview(Guid id, CancellationToken cancellationToken)
    {
        var source = await _dbContext.InvoiceSourceConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (source == null)
        {
            return NotFound();
        }

        try
        {
            var service = _serviceFactory.GetService(source.SourceType);
            var preview = await service.GetSyncPreviewAsync(id, cancellationToken);

            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo preview de fuente {SourceId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Valida una configuración sin guardarla.
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateSourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ConfigJson))
            {
                return BadRequest(new { error = "La configuración JSON es requerida" });
            }

            var service = _serviceFactory.GetService(request.SourceType);
            var result = await service.ValidateConfigAsync(request.ConfigJson, cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Error validando configuración - tipo de fuente inválido");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando configuración");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sincroniza todas las fuentes habilitadas.
    /// </summary>
    [HttpPost("sync-all")]
    public async Task<IActionResult> SyncAll(CancellationToken cancellationToken, [FromQuery] bool forceReprocess = false)
    {
        try
        {
            var results = await _syncUseCase.ExecuteAsync(forceReprocess, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando todas las fuentes");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reanuda la sincronización pausada de una fuente específica (solo para OneDrive actualmente).
    /// </summary>
    [HttpPost("{id:guid}/sync/resume")]
    public IActionResult ResumeSync(Guid id)
    {
        var source = _dbContext.InvoiceSourceConfigs.Find(id);
        if (source == null)
        {
            return NotFound();
        }

        // Solo OneDrive soporta pausa/reanudación actualmente
        if (source.SourceType != InvoiceSourceType.OneDrive)
        {
            return BadRequest(new { error = "Reanudación solo disponible para fuentes de OneDrive" });
        }

        try
        {
            // Obtener el servicio de OneDrive directamente para acceder a métodos de pausa/reanudación
            var oneDriveService = HttpContext.RequestServices.GetRequiredService<FresiaFlow.Adapters.Outbound.OneDrive.IOneDriveSyncService>();
            
            if (!oneDriveService.IsSyncInProgress)
            {
                return BadRequest(new { error = "No hay ninguna sincronización en progreso" });
            }

            if (!oneDriveService.IsSyncPaused)
            {
                return BadRequest(new { error = "La sincronización no está pausada" });
            }

            _logger.LogInformation("Reanudando sincronización de OneDrive (fuente {SourceId})", id);
            oneDriveService.ResumeCurrentSync();
            return Ok(new { message = "Sincronización reanudada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reanudando sincronización de fuente {SourceId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<SourceValidationResult> ValidateSourceConfigAsync(InvoiceSourceConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var service = _serviceFactory.GetService(config.SourceType);
            return await service.ValidateConfigAsync(config.ConfigJson, cancellationToken);
        }
        catch (NotImplementedException ex)
        {
            return new SourceValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
        catch (ArgumentException ex)
        {
            return new SourceValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Normaliza el JSON de configuración de OneDrive para preservar syncIntervalMinutes.
    /// PRIORIDAD: Si el nuevo JSON tiene syncIntervalMinutes válido, usarlo. Si no, preservar el existente.
    /// </summary>
    private string NormalizeOneDriveConfigJson(string existingConfigJson, string newConfigJson)
    {
        try
        {
            // Parsear ambos JSONs
            using var existingJson = JsonDocument.Parse(existingConfigJson);
            using var newJson = JsonDocument.Parse(newConfigJson);

            // Obtener syncIntervalMinutes del JSON existente (desde la BD)
            int existingInterval = 15; // Default solo si no existe en BD
            if (existingJson.RootElement.TryGetProperty("syncIntervalMinutes", out var existingIntervalProp))
            {
                if (existingIntervalProp.ValueKind == JsonValueKind.Number)
                {
                    existingInterval = existingIntervalProp.GetInt32();
                    _logger.LogDebug("Intervalo existente en BD: {Existing}", existingInterval);
                }
                else if (existingIntervalProp.ValueKind == JsonValueKind.String)
                {
                    if (int.TryParse(existingIntervalProp.GetString(), out var parsedExisting))
                    {
                        existingInterval = parsedExisting;
                        _logger.LogDebug("Intervalo existente en BD (desde string): {Existing}", existingInterval);
                    }
                }
            }

            // Obtener syncIntervalMinutes del nuevo JSON (del frontend)
            int finalInterval = existingInterval; // Por defecto, preservar el existente
            bool hasValidNewInterval = false;
            
            if (newJson.RootElement.TryGetProperty("syncIntervalMinutes", out var newIntervalProp))
            {
                int? parsedNewInterval = null;
                
                if (newIntervalProp.ValueKind == JsonValueKind.Number)
                {
                    parsedNewInterval = newIntervalProp.GetInt32();
                }
                else if (newIntervalProp.ValueKind == JsonValueKind.String)
                {
                    if (int.TryParse(newIntervalProp.GetString(), out var parsedValue))
                    {
                        parsedNewInterval = parsedValue;
                    }
                }

                // Validar que el valor esté en un rango válido (1-1440 minutos)
                if (parsedNewInterval.HasValue && parsedNewInterval.Value >= 1 && parsedNewInterval.Value <= 1440)
                {
                    finalInterval = parsedNewInterval.Value;
                    hasValidNewInterval = true;
                    _logger.LogInformation("✅ Usando NUEVO valor de syncIntervalMinutes: {NewValue} (reemplazando existente: {Existing})", 
                        finalInterval, existingInterval);
                }
                else if (parsedNewInterval.HasValue)
                {
                    _logger.LogWarning("⚠️ syncIntervalMinutes fuera de rango ({Value}), preservando valor existente: {Existing}", 
                        parsedNewInterval.Value, existingInterval);
                }
            }

            if (!hasValidNewInterval)
            {
                _logger.LogInformation("ℹ️ syncIntervalMinutes no encontrado o inválido en nuevo JSON, preservando valor existente: {Existing}", existingInterval);
            }

            // Deserializar el nuevo JSON a un diccionario para modificarlo
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNameCaseInsensitive = true
            };

            var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(newConfigJson, jsonOptions);
            if (configDict == null)
            {
                _logger.LogWarning("⚠️ No se pudo deserializar el nuevo JSON, usando JSON original");
                return newConfigJson;
            }

            // SIEMPRE actualizar syncIntervalMinutes con el valor final calculado
            var intervalElement = JsonSerializer.SerializeToElement(finalInterval, jsonOptions);
            configDict["syncIntervalMinutes"] = intervalElement;
            
            var normalizedJson = JsonSerializer.Serialize(configDict, jsonOptions);
            _logger.LogInformation("💾 JSON normalizado para OneDrive. syncIntervalMinutes FINAL: {Interval} (existente: {Existing}, nuevo válido: {HasNew})", 
                finalInterval, existingInterval, hasValidNewInterval);
            
            return normalizedJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error normalizando JSON de OneDrive, usando JSON original");
            return newConfigJson;
        }
    }
}

public class CreateOrUpdateSourceRequest
{
    public Guid? Id { get; set; }
    public InvoiceSourceType SourceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
    public bool? Enabled { get; set; }
}

public class ValidateSourceRequest
{
    public InvoiceSourceType SourceType { get; set; }
    public string ConfigJson { get; set; } = string.Empty;
}

