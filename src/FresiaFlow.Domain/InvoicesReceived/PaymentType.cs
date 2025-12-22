namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Tipo de pago de una factura recibida.
/// </summary>
public enum PaymentType
{
    /// <summary>
    /// Pago realizado mediante transferencia bancaria o similar.
    /// La factura está relacionada con uno o varios movimientos bancarios.
    /// </summary>
    Bank = 0,

    /// <summary>
    /// Pago realizado en efectivo.
    /// La factura no tiene movimientos bancarios asociados.
    /// </summary>
    Cash = 1
}

