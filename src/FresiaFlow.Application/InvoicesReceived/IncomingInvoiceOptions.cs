namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Opciones de configuración para el procesamiento de facturas entrantes.
/// </summary>
public class IncomingInvoiceOptions
{
    public const string SectionName = "IncomingInvoices";

    /// <summary>
    /// Carpeta donde se depositan las facturas PDF a procesar.
    /// </summary>
    public string WatchFolder { get; set; } = "./incoming-invoices";

    /// <summary>
    /// Carpeta donde se mueven las facturas procesadas exitosamente.
    /// </summary>
    public string ProcessedSuccessFolder { get; set; } = "./processed/success";

    /// <summary>
    /// Carpeta donde se mueven las facturas con errores.
    /// </summary>
    public string ProcessedErrorFolder { get; set; } = "./processed/error";

    /// <summary>
    /// Intervalo de escaneo en segundos (por si FileSystemWatcher falla).
    /// </summary>
    public int ScanIntervalSeconds { get; set; } = 60;
}

