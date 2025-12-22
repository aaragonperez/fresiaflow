namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Origen de la factura recibida.
/// </summary>
public enum InvoiceOrigin
{
    /// <summary>
    /// Factura subida manualmente desde PDF o imagen.
    /// </summary>
    ManualUpload = 0,

    /// <summary>
    /// Factura recibida por email y procesada automáticamente.
    /// </summary>
    Email = 1,

    /// <summary>
    /// Factura creada manualmente en el sistema.
    /// </summary>
    ManualEntry = 2
}

