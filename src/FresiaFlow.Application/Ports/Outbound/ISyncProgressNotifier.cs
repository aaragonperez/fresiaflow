namespace FresiaFlow.Application.Ports.Outbound;

/// <summary>
/// Puerto para notificar progreso de sincronizaciones a la capa driver (ej. SignalR).
/// </summary>
public interface ISyncProgressNotifier
{
    Task NotifyAsync(SyncProgressUpdate update, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO de progreso de sincronización reutilizable por los adaptadores.
/// </summary>
public class SyncProgressUpdate
{
    public string CurrentFile { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int Percentage { get; set; }
    public string Status { get; set; } = string.Empty; // "syncing", "completed", "error", "cancelled", "paused"
    public string? Message { get; set; }
    
    // Información adicional para mayor detalle
    public Guid? SourceId { get; set; } // ID de la fuente que se está sincronizando
    public string? SourceName { get; set; } // Nombre de la fuente
    public string? SourceType { get; set; } // Tipo de fuente (Email, Portal, WebScraping, OneDrive)
    public string? Stage { get; set; } // Etapa actual: "initializing", "listing", "downloading", "processing", "extracting", "saving", "completed"
    
    // Estadísticas detalladas
    public int ProcessedFiles { get; set; } // Archivos procesados exitosamente
    public int FailedFiles { get; set; } // Archivos que fallaron
    public int SkippedFiles { get; set; } // Archivos omitidos (ya sincronizados)
    public int AlreadyExistedFiles { get; set; } // Archivos que ya existían en BD
    
    // Información del archivo actual
    public string? CurrentFileName { get; set; } // Nombre del archivo actual
    public string? CurrentFileStatus { get; set; } // Estado del archivo actual: "downloading", "processing", "extracting", "saving", "completed", "failed", "skipped"
    public long? CurrentFileSize { get; set; } // Tamaño del archivo actual en bytes
    public string? CurrentFileError { get; set; } // Error del archivo actual si falló
}

