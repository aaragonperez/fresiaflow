using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Línea de detalle de una factura recibida.
/// </summary>
public class InvoiceReceivedLine
{
    public Guid Id { get; private set; }
    public Guid InvoiceReceivedId { get; private set; }
    public int LineNumber { get; private set; }
    public string Description { get; private set; }
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public decimal? TaxRate { get; private set; }
    public Money LineTotal { get; private set; }

    // Navegación
    public InvoiceReceived InvoiceReceived { get; private set; } = null!;

    // Constructor privado para EF Core
    private InvoiceReceivedLine() 
    {
        Description = string.Empty;
        UnitPrice = new Money(0, "EUR");
        LineTotal = new Money(0, "EUR");
    }

    public InvoiceReceivedLine(
        int lineNumber,
        string description,
        decimal quantity,
        Money unitPrice,
        Money lineTotal)
    {
        if (lineNumber <= 0)
            throw new ArgumentException("El número de línea debe ser positivo.", nameof(lineNumber));
        
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede estar vacía.", nameof(description));
        
        if (quantity <= 0)
            throw new ArgumentException("La cantidad debe ser positiva.", nameof(quantity));
        
        if (unitPrice.Value < 0)
            throw new ArgumentException("El precio unitario no puede ser negativo.", nameof(unitPrice));
        
        if (lineTotal.Value < 0)
            throw new ArgumentException("El total de línea no puede ser negativo.", nameof(lineTotal));

        Id = Guid.NewGuid();
        LineNumber = lineNumber;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
    }

    public void SetTaxRate(decimal? taxRate)
    {
        if (taxRate.HasValue && (taxRate.Value < 0 || taxRate.Value > 100))
            throw new ArgumentException("La tasa de impuesto debe estar entre 0 y 100.", nameof(taxRate));
        
        TaxRate = taxRate;
    }
}

