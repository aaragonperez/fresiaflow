namespace FresiaFlow.Adapters.Inbound.Api.Dtos;

/// <summary>
/// DTO completo para factura recibida.
/// Refleja el modelo contable real sin estados ficticios.
/// </summary>
public class InvoiceReceivedDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    
    // Proveedor
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierTaxId { get; set; }
    public string? SupplierAddress { get; set; }
    
    // Importes
    public decimal SubtotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    
    // Pago
    public string PaymentType { get; set; } = "Cash"; // "Bank" o "Cash"
    public List<InvoiceReceivedPaymentDto> Payments { get; set; } = new();
    
    // Metadatos
    public string Origin { get; set; } = "ManualUpload";
    public string OriginalFilePath { get; set; } = string.Empty;
    public string? ProcessedFilePath { get; set; }
    public decimal? ExtractionConfidence { get; set; }
    public string? Notes { get; set; }
    
    // Líneas
    public List<InvoiceReceivedLineDto> Lines { get; set; } = new();
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InvoiceReceivedPaymentDto
{
    public Guid Id { get; set; }
    public Guid BankTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime PaymentDate { get; set; }
}

public class InvoiceReceivedLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string UnitPriceCurrency { get; set; } = "EUR";
    public decimal? TaxRate { get; set; }
    public decimal LineTotal { get; set; }
    public string LineTotalCurrency { get; set; } = "EUR";
}

