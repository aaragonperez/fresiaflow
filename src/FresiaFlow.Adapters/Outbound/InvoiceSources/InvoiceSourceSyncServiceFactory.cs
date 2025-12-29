using FresiaFlow.Domain.Sync;

namespace FresiaFlow.Adapters.Outbound.InvoiceSources;

/// <summary>
/// Factory para obtener el servicio de sincronización correcto según el tipo de fuente.
/// </summary>
public class InvoiceSourceSyncServiceFactory
{
    private readonly EmailSyncService _emailSyncService;
    private readonly WebScrapingSyncService _webScrapingSyncService;
    private readonly PortalSyncService _portalSyncService;
    private readonly OneDriveInvoiceSourceSyncService _oneDriveSyncService;

    public InvoiceSourceSyncServiceFactory(
        EmailSyncService emailSyncService,
        WebScrapingSyncService webScrapingSyncService,
        PortalSyncService portalSyncService,
        OneDriveInvoiceSourceSyncService oneDriveSyncService)
    {
        _emailSyncService = emailSyncService;
        _webScrapingSyncService = webScrapingSyncService;
        _portalSyncService = portalSyncService;
        _oneDriveSyncService = oneDriveSyncService;
    }

    public IInvoiceSourceSyncService GetService(InvoiceSourceType sourceType)
    {
        return sourceType switch
        {
            InvoiceSourceType.Email => _emailSyncService,
            InvoiceSourceType.WebScraping => _webScrapingSyncService,
            InvoiceSourceType.Portal => _portalSyncService,
            InvoiceSourceType.OneDrive => _oneDriveSyncService,
            _ => throw new ArgumentException($"Tipo de fuente desconocido: {sourceType}")
        };
    }
}

