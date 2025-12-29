using FresiaFlow.Domain.Sync;

namespace FresiaFlow.Adapters.Outbound.InvoiceSources;

/// <summary>
/// Interfaz común para servicios de sincronización de facturas desde diferentes fuentes.
/// </summary>
public interface IInvoiceSourceSyncService
{
    /// <summary>
    /// Obtiene la configuración de la fuente.
    /// </summary>
    Task<InvoiceSourceConfig?> GetConfigAsync(Guid sourceId);

    /// <summary>
    /// Guarda o actualiza la configuración de la fuente.
    /// </summary>
    Task<InvoiceSourceConfig> SaveConfigAsync(InvoiceSourceConfig config);

    /// <summary>
    /// Obtiene un preview de las facturas disponibles sin descargarlas.
    /// </summary>
    Task<SyncPreview> GetSyncPreviewAsync(Guid sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sincroniza facturas desde la fuente.
    /// </summary>
    Task<SyncResult> SyncNowAsync(Guid sourceId, bool forceReprocess = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida la configuración de la fuente.
    /// </summary>
    Task<SourceValidationResult> ValidateConfigAsync(string configJson, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de validación de configuración.
/// </summary>
public class SourceValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Info { get; set; }
}

/// <summary>
/// Preview de sincronización.
/// </summary>
public class SyncPreview
{
    public int TotalFiles { get; set; }
    public int SupportedFiles { get; set; }
    public int AlreadySynced { get; set; }
    public int PendingToProcess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Resultado de sincronización.
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> DetailedErrors { get; set; } = new();
}

