using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Entidad que representa una factura recibida procesada automáticamente.
/// </summary>
public class InvoiceReceived
{
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; }
    public string SupplierName { get; private set; }
    public string? SupplierTaxId { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Money TotalAmount { get; private set; }
    public Money? TaxAmount { get; private set; }
    public Money? SubtotalAmount { get; private set; }
    public string Currency { get; private set; }
    public string OriginalFilePath { get; private set; }
    public string? ProcessedFilePath { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public InvoiceReceivedStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<InvoiceReceivedLine> _lines = new();
    public IReadOnlyCollection<InvoiceReceivedLine> Lines => _lines.AsReadOnly();

    // Constructor privado para EF Core
    private InvoiceReceived() 
    {
        InvoiceNumber = string.Empty;
        SupplierName = string.Empty;
        Currency = string.Empty;
        OriginalFilePath = string.Empty;
        TotalAmount = new Money(0, "EUR");
    }

    public InvoiceReceived(
        string invoiceNumber,
        string supplierName,
        DateTime issueDate,
        Money totalAmount,
        string currency,
        string originalFilePath)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("El número de factura no puede estar vacío.", nameof(invoiceNumber));
        
        if (string.IsNullOrWhiteSpace(supplierName))
            throw new ArgumentException("El nombre del proveedor no puede estar vacío.", nameof(supplierName));
        
        if (totalAmount.Value < 0)
            throw new ArgumentException("El total de la factura no puede ser negativo.", nameof(totalAmount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("La moneda no puede estar vacía.", nameof(currency));
        
        if (string.IsNullOrWhiteSpace(originalFilePath))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(originalFilePath));

        Id = Guid.NewGuid();
        InvoiceNumber = invoiceNumber;
        SupplierName = supplierName;
        IssueDate = issueDate;
        TotalAmount = totalAmount;
        Currency = currency;
        OriginalFilePath = originalFilePath;
        ProcessedAt = DateTime.UtcNow;
        Status = InvoiceReceivedStatus.Processed;
    }

    public void AddLine(InvoiceReceivedLine line)
    {
        if (line == null)
            throw new ArgumentNullException(nameof(line));
        
        _lines.Add(line);
    }

    public void SetSupplierTaxId(string taxId)
    {
        if (!string.IsNullOrWhiteSpace(taxId))
            SupplierTaxId = taxId;
    }

    public void SetDueDate(DateTime? dueDate)
    {
        if (dueDate.HasValue && dueDate.Value < IssueDate)
            throw new ArgumentException("La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
        
        DueDate = dueDate;
    }

    public void SetTaxAmount(Money? taxAmount)
    {
        if (taxAmount != null && taxAmount.Value < 0)
            throw new ArgumentException("El importe de impuestos no puede ser negativo.");
        
        TaxAmount = taxAmount;
    }

    public void SetSubtotalAmount(Money? subtotalAmount)
    {
        if (subtotalAmount != null && subtotalAmount.Value < 0)
            throw new ArgumentException("El subtotal no puede ser negativo.");
        
        SubtotalAmount = subtotalAmount;
    }

    public void SetProcessedFilePath(string processedFilePath)
    {
        if (!string.IsNullOrWhiteSpace(processedFilePath))
            ProcessedFilePath = processedFilePath;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
    }

    public void MarkAsError(string errorMessage)
    {
        Status = InvoiceReceivedStatus.Error;
        Notes = errorMessage;
    }

    public void MarkAsReviewed()
    {
        Status = InvoiceReceivedStatus.Reviewed;
    }
}

