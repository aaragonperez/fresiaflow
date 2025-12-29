namespace FresiaFlow.Domain.Sync;

/// <summary>
/// Configuración de sincronización con OneDrive
/// </summary>
public class OneDriveSyncConfig
{
    public Guid Id { get; private set; }
    public bool Enabled { get; private set; }
    public string? TenantId { get; private set; }
    public string? ClientId { get; private set; }
    public string? ClientSecret { get; private set; }
    public string? FolderPath { get; private set; } // Ruta de la carpeta en OneDrive
    public string? DriveId { get; private set; } // ID del drive (para SharePoint/Teams)
    public int SyncIntervalMinutes { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncError { get; private set; }
    public int TotalFilesSynced { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private OneDriveSyncConfig() { }

    public static OneDriveSyncConfig CreateDefault()
    {
        return new OneDriveSyncConfig
        {
            Id = Guid.NewGuid(),
            Enabled = false,
            SyncIntervalMinutes = 15,
            TotalFilesSynced = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateCredentials(string tenantId, string clientId, string clientSecret)
    {
        TenantId = tenantId;
        ClientId = clientId;
        ClientSecret = clientSecret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCredentialsWithoutSecret(string tenantId, string clientId)
    {
        TenantId = tenantId;
        ClientId = clientId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFolderSettings(string folderPath, string? driveId = null)
    {
        FolderPath = folderPath;
        DriveId = driveId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSyncInterval(int minutes)
    {
        if (minutes < 1) minutes = 1;
        if (minutes > 1440) minutes = 1440; // Max 24 horas
        SyncIntervalMinutes = minutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        Enabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        Enabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSuccessfulSync(int filesProcessed)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = null;
        TotalFilesSynced += filesProcessed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedSync(string error)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(TenantId) 
            && !string.IsNullOrEmpty(ClientId) 
            && !string.IsNullOrEmpty(ClientSecret)
            && !string.IsNullOrEmpty(FolderPath);
    }
}


