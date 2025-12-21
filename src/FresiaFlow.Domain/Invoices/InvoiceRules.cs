namespace FresiaFlow.Domain.Invoices;

/// <summary>
/// Reglas de negocio para facturas.
/// El dominio define estas reglas sin depender de infraestructura.
/// </summary>
public static class InvoiceRules
{
    /// <summary>
    /// Determina si una factura puede ser conciliada con una transacción bancaria.
    /// Tolerancia: ±5% del monto o ±3 días de diferencia en fechas.
    /// </summary>
    public static bool CanReconcile(Invoice invoice, decimal transactionAmount, DateTime transactionDate)
    {
        if (invoice.Status == InvoiceStatus.Paid)
            return false;

        if (invoice.Status == InvoiceStatus.Cancelled)
            return false;

        // Tolerancia de monto: ±5%
        var amountDifference = Math.Abs(invoice.Amount.Value - transactionAmount);
        var tolerance = invoice.Amount.Value * 0.05m;
        if (amountDifference > tolerance)
            return false;

        // Tolerancia de fecha: ±3 días
        var invoiceDate = (invoice.DueDate ?? invoice.IssueDate).Date;
        var transactionDateOnly = transactionDate.Date;
        var dateDifference = invoiceDate > transactionDateOnly 
            ? (invoiceDate - transactionDateOnly).Days 
            : (transactionDateOnly - invoiceDate).Days;
        if (dateDifference > 3)
            return false;

        return true;
    }

    /// <summary>
    /// Calcula el score de coincidencia para conciliación automática.
    /// </summary>
    public static decimal CalculateMatchScore(Invoice invoice, decimal transactionAmount, DateTime transactionDate)
    {
        var amountScore = 1.0m - (Math.Abs(invoice.Amount.Value - transactionAmount) / invoice.Amount.Value);
        
        var invoiceDate = (invoice.DueDate ?? invoice.IssueDate).Date;
        var transactionDateOnly = transactionDate.Date;
        var daysDifference = invoiceDate > transactionDateOnly 
            ? (invoiceDate - transactionDateOnly).Days 
            : (transactionDateOnly - invoiceDate).Days;
        
        var dateScore = 1.0m - (daysDifference / 30.0m);
        
        return (amountScore * 0.7m) + (dateScore * 0.3m);
    }
}

