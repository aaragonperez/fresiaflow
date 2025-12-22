using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Entidad que representa una factura recibida de un proveedor.
/// Modelo contable: toda factura recibida está contabilizada desde su recepción.
/// No existen estados de "pendiente" o "en proceso".
/// </summary>
public class InvoiceReceived
{
    // Identificación
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime ReceivedDate { get; private set; } // Fecha de recepción (contable)

    // Proveedor
    public string SupplierName { get; private set; }
    public string? SupplierTaxId { get; private set; } // NIF/CIF
    public string? SupplierAddress { get; private set; } // Dirección fiscal

    // Importes
    public Money SubtotalAmount { get; private set; } // Base imponible
    public Money? TaxAmount { get; private set; } // IVA
    public decimal? TaxRate { get; private set; } // Tipo de IVA (21%, 10%, etc.)
    public Money TotalAmount { get; private set; } // Total factura
    public string Currency { get; private set; }

    // Pago
    public PaymentType PaymentType { get; private set; }
    private readonly List<InvoiceReceivedPayment> _payments = new();
    public IReadOnlyCollection<InvoiceReceivedPayment> Payments => _payments.AsReadOnly();

    // Metadatos
    public InvoiceOrigin Origin { get; private set; }
    public string OriginalFilePath { get; private set; } // Ruta del PDF/imagen original
    public string? ProcessedFilePath { get; private set; } // Ruta del archivo procesado
    public decimal? ExtractionConfidence { get; private set; } // Confianza de extracción IA (0-1)
    public string? Notes { get; private set; }

    // Líneas de detalle
    private readonly List<InvoiceReceivedLine> _lines = new();
    public IReadOnlyCollection<InvoiceReceivedLine> Lines => _lines.AsReadOnly();

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Constructor privado para EF Core
    private InvoiceReceived() 
    {
        InvoiceNumber = string.Empty;
        SupplierName = string.Empty;
        Currency = "EUR";
        OriginalFilePath = string.Empty;
        SubtotalAmount = new Money(0, "EUR");
        TotalAmount = new Money(0, "EUR");
    }

    /// <summary>
    /// Crea una nueva factura recibida.
    /// Por defecto se asume pago en efectivo hasta que se asocie un movimiento bancario.
    /// </summary>
    public InvoiceReceived(
        string invoiceNumber,
        string supplierName,
        DateTime issueDate,
        DateTime receivedDate,
        Money subtotalAmount,
        Money totalAmount,
        string currency,
        InvoiceOrigin origin,
        string originalFilePath)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("El número de factura no puede estar vacío.", nameof(invoiceNumber));
        
        if (string.IsNullOrWhiteSpace(supplierName))
            throw new ArgumentException("El nombre del proveedor no puede estar vacío.", nameof(supplierName));
        
        if (subtotalAmount.Value < 0)
            throw new ArgumentException("La base imponible no puede ser negativa.", nameof(subtotalAmount));
        
        if (totalAmount.Value < 0)
            throw new ArgumentException("El total de la factura no puede ser negativo.", nameof(totalAmount));
        
        if (totalAmount.Value < subtotalAmount.Value)
            throw new ArgumentException("El total no puede ser menor que la base imponible.", nameof(totalAmount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("La moneda no puede estar vacía.", nameof(currency));
        
        if (string.IsNullOrWhiteSpace(originalFilePath))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(originalFilePath));

        Id = Guid.NewGuid();
        InvoiceNumber = invoiceNumber;
        SupplierName = supplierName;
        IssueDate = issueDate;
        ReceivedDate = receivedDate;
        SubtotalAmount = subtotalAmount;
        TotalAmount = totalAmount;
        Currency = currency;
        Origin = origin;
        OriginalFilePath = originalFilePath;
        PaymentType = PaymentType.Cash; // Por defecto efectivo hasta que se asocie banco
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Métodos de negocio

    /// <summary>
    /// Asocia un pago bancario a esta factura.
    /// Cambia automáticamente el tipo de pago a Bank si no hay pagos previos.
    /// </summary>
    public void AddBankPayment(InvoiceReceivedPayment payment)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));
        
        if (payment.InvoiceReceivedId != Id)
            throw new ArgumentException("El pago no pertenece a esta factura.", nameof(payment));

        _payments.Add(payment);
        
        // Si es el primer pago bancario, cambiar tipo de pago
        if (PaymentType == PaymentType.Cash && _payments.Count == 1)
        {
            PaymentType = PaymentType.Bank;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Elimina un pago bancario.
    /// Si no quedan pagos, cambia el tipo de pago a Cash.
    /// </summary>
    public void RemoveBankPayment(Guid paymentId)
    {
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment == null)
            return;

        _payments.Remove(payment);

        // Si no quedan pagos bancarios, cambiar a efectivo
        if (_payments.Count == 0)
        {
            PaymentType = PaymentType.Cash;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula el total pagado mediante movimientos bancarios.
    /// </summary>
    public Money GetTotalPaidByBank()
    {
        if (_payments.Count == 0)
            return new Money(0, Currency);

        return _payments.Aggregate(
            new Money(0, Currency),
            (sum, payment) => sum + payment.Amount);
    }

    /// <summary>
    /// Verifica si la factura está completamente pagada mediante banco.
    /// </summary>
    public bool IsFullyPaidByBank()
    {
        if (PaymentType != PaymentType.Bank || _payments.Count == 0)
            return false;

        var totalPaid = GetTotalPaidByBank();
        return totalPaid.Value >= TotalAmount.Value;
    }

    // Setters para campos opcionales

    public void SetSupplierTaxId(string? taxId)
    {
        SupplierTaxId = taxId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSupplierAddress(string? address)
    {
        SupplierAddress = address;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTaxAmount(Money? taxAmount)
    {
        if (taxAmount != null && taxAmount.Value < 0)
            throw new ArgumentException("El importe de impuestos no puede ser negativo.", nameof(taxAmount));
        
        TaxAmount = taxAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTaxRate(decimal? taxRate)
    {
        if (taxRate.HasValue && (taxRate.Value < 0 || taxRate.Value > 100))
            throw new ArgumentException("La tasa de impuesto debe estar entre 0 y 100.", nameof(taxRate));
        
        TaxRate = taxRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetProcessedFilePath(string? processedFilePath)
    {
        ProcessedFilePath = processedFilePath;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetExtractionConfidence(decimal? confidence)
    {
        if (confidence.HasValue && (confidence.Value < 0 || confidence.Value > 1))
            throw new ArgumentException("La confianza debe estar entre 0 y 1.", nameof(confidence));
        
        ExtractionConfidence = confidence;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLine(InvoiceReceivedLine line)
    {
        if (line == null)
            throw new ArgumentNullException(nameof(line));
        
        // Establecer la relación usando reflexión (InvoiceReceivedId es privado)
        var invoiceIdProperty = typeof(InvoiceReceivedLine).GetProperty("InvoiceReceivedId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (invoiceIdProperty != null && invoiceIdProperty.CanWrite)
        {
            invoiceIdProperty.SetValue(line, Id);
        }
        
        _lines.Add(line);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSupplierName(string supplierName)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
            throw new ArgumentException("El nombre del proveedor no puede estar vacío.", nameof(supplierName));
        
        SupplierName = supplierName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetInvoiceNumber(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("El número de factura no puede estar vacío.", nameof(invoiceNumber));
        
        InvoiceNumber = invoiceNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetIssueDate(DateTime issueDate)
    {
        IssueDate = issueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetReceivedDate(DateTime receivedDate)
    {
        ReceivedDate = receivedDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSubtotalAmount(Money subtotalAmount)
    {
        if (subtotalAmount.Value < 0)
            throw new ArgumentException("La base imponible no puede ser negativa.", nameof(subtotalAmount));
        
        if (TotalAmount.Value < subtotalAmount.Value)
            throw new ArgumentException("La base imponible no puede ser mayor que el total.", nameof(subtotalAmount));
        
        SubtotalAmount = subtotalAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTotalAmount(Money totalAmount)
    {
        if (totalAmount.Value < 0)
            throw new ArgumentException("El total de la factura no puede ser negativo.", nameof(totalAmount));
        
        if (totalAmount.Value < SubtotalAmount.Value)
            throw new ArgumentException("El total no puede ser menor que la base imponible.", nameof(totalAmount));
        
        TotalAmount = totalAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("La moneda no puede estar vacía.", nameof(currency));
        
        Currency = currency;
        UpdatedAt = DateTime.UtcNow;
    }
}
