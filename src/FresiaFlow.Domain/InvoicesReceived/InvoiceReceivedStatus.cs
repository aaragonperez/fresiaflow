namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Estados posibles de una factura recibida.
/// </summary>
public enum InvoiceReceivedStatus
{
    Processed = 0,
    Reviewed = 1,
    Error = 2
}

