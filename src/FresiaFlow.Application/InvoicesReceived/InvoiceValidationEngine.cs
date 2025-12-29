using FresiaFlow.Domain.InvoicesReceived;

namespace FresiaFlow.Application.InvoicesReceived;

/// <summary>
/// Reglas deterministas flexibles para validar los totales de una factura.
/// </summary>
public static class InvoiceValidationEngine
{
    public static InvoiceValidationResult Validate(
        InvoiceExtractionResultDto data,
        InvoiceProcessingOptions options)
    {
        var issues = new List<string>();

        // Permitir importes negativos (notas de crédito, rectificaciones, etc.)
        // Solo advertir si es cero, no si es negativo
        if (data.TotalAmount == 0)
        {
            issues.Add("Total es cero. Verifica que sea correcto.");
        }

        var issueDate = data.GetIssueDate();
        if (issueDate > DateTime.UtcNow.AddDays(1))
        {
            issues.Add("Fecha de emisión futura.");
        }

        var subtotal = data.SubtotalAmount
            ?? data.TotalAmount - (data.TaxAmount ?? 0m) + (data.IrpfAmount ?? 0m);

        var recomputedTotal = subtotal + (data.TaxAmount ?? 0m) - (data.IrpfAmount ?? 0m);
        if (Math.Abs(recomputedTotal - data.TotalAmount) > options.TotalTolerance)
        {
            issues.Add("Totales no cuadran con la tolerancia permitida.");
        }

        if (data.TaxAmount.HasValue && data.TaxRate.HasValue)
        {
            var expectedTax = Math.Round(subtotal * data.TaxRate.Value / 100m, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(expectedTax - data.TaxAmount.Value) > options.TotalTolerance)
            {
                issues.Add("El IVA calculado no coincide con el monto declarado.");
            }
        }

        var status = issues.Count == 0
            ? InvoiceValidationStatus.Ok
            : InvoiceValidationStatus.Doubtful;

        return new InvoiceValidationResult(status, issues.AsReadOnly());
    }
}

