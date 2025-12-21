namespace FresiaFlow.Domain.Invoices;

/// <summary>
/// Estados posibles de una factura en el sistema.
/// </summary>
public enum InvoiceStatus
{
    Pending = 0,
    Paid = 1,
    Overdue = 2,
    Cancelled = 3
}

