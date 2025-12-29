namespace FresiaFlow.Domain.Sync;

/// <summary>
/// Representa un archivo sincronizado desde una fuente externa (OneDrive, etc.)
/// </summary>
public class SyncedFile
{
    public Guid Id { get; private set; }
    public string Source { get; private set; } = string.Empty; // "OneDrive", "GoogleDrive", etc.
    public string ExternalId { get; private set; } = string.Empty; // ID del archivo en el sistema externo
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty; // Ruta en el sistema externo
    public long FileSize { get; private set; }
    public string FileHash { get; private set; } = string.Empty; // Hash para detectar cambios
    public DateTime ExternalModifiedDate { get; private set; }
    public DateTime SyncedAt { get; private set; }
    public SyncStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? InvoiceId { get; private set; } // Referencia a la factura creada

    private SyncedFile() { }

    public static SyncedFile Create(
        string source,
        string externalId,
        string fileName,
        string filePath,
        long fileSize,
        string fileHash,
        DateTime externalModifiedDate)
    {
        return new SyncedFile
        {
            Id = Guid.NewGuid(),
            Source = source,
            ExternalId = externalId,
            FileName = fileName,
            FilePath = filePath,
            FileSize = fileSize,
            FileHash = fileHash,
            ExternalModifiedDate = externalModifiedDate,
            SyncedAt = DateTime.UtcNow,
            Status = SyncStatus.Pending
        };
    }

    public void MarkAsProcessing()
    {
        Status = SyncStatus.Processing;
        ErrorMessage = null;
    }

    public void MarkAsCompleted(Guid invoiceId)
    {
        Status = SyncStatus.Completed;
        InvoiceId = invoiceId;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string error)
    {
        Status = SyncStatus.Failed;
        ErrorMessage = error;
    }

    public void MarkAsSkipped(string reason)
    {
        Status = SyncStatus.Skipped;
        ErrorMessage = reason;
    }

    public void UpdateHash(string newHash, DateTime modifiedDate)
    {
        FileHash = newHash;
        ExternalModifiedDate = modifiedDate;
        SyncedAt = DateTime.UtcNow;
    }

    public void UpdateSource(string newSource)
    {
        Source = newSource;
    }
}

public enum SyncStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Skipped
}


