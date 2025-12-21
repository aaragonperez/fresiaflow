using FresiaFlow.Domain.Invoices;

namespace FresiaFlow.Application.Policies;

/// <summary>
/// Política de aprobación para facturas.
/// Define reglas de negocio sobre cuándo una factura requiere aprobación.
/// </summary>
public static class ApprovalPolicy
{
    /// <summary>
    /// Determina si una factura requiere aprobación manual.
    /// </summary>
    public static bool RequiresApproval(Invoice invoice)
    {
        // Facturas mayores a 1000 EUR requieren aprobación
        if (invoice.Amount.Value > 1000m)
            return true;

        // Facturas de proveedores nuevos requieren aprobación
        // TODO: Implementar lógica de "proveedor nuevo"

        return false;
    }

    /// <summary>
    /// Determina si una factura puede ser pagada automáticamente.
    /// </summary>
    public static bool CanAutoPay(Invoice invoice)
    {
        if (RequiresApproval(invoice))
            return false;

        // Facturas pequeñas y recurrentes pueden pagarse automáticamente
        if (invoice.Amount.Value < 500m)
            return true;

        return false;
    }
}

