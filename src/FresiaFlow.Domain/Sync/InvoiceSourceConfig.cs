namespace FresiaFlow.Domain.Sync;

/// <summary>
/// Configuración de una fuente de descarga automática de facturas.
/// Soporta múltiples tipos: Email, Portal, WebScraping, OneDrive.
/// </summary>
public class InvoiceSourceConfig
{
    public Guid Id { get; private set; }
    public InvoiceSourceType SourceType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ConfigJson { get; private set; } = string.Empty; // Configuración específica serializada como JSON
    public bool Enabled { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncError { get; private set; }
    public int TotalFilesSynced { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private InvoiceSourceConfig() { }

    public static InvoiceSourceConfig Create(
        InvoiceSourceType sourceType,
        string name,
        string configJson)
    {
        return new InvoiceSourceConfig
        {
            Id = Guid.NewGuid(),
            SourceType = sourceType,
            Name = name,
            ConfigJson = configJson,
            Enabled = false,
            TotalFilesSynced = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateConfig(string name, string configJson)
    {
        Name = name;
        ConfigJson = configJson;
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
}

public enum InvoiceSourceType
{
    Email,
    Portal,
    WebScraping,
    OneDrive
}

