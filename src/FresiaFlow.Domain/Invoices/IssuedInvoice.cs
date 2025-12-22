using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.Invoices;

/// <summary>
/// Entidad que representa una factura emitida por la empresa.
/// Incluye información completa del cliente y dirección.
/// </summary>
public class IssuedInvoice
{
    public Guid Id { get; private set; }
    public string Series { get; private set; } = string.Empty;
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public Money TaxableBase { get; private set; } = null!;
    public Money TaxAmount { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    
    // Información del cliente
    public string ClientId { get; private set; } = string.Empty;
    public string ClientTaxId { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty;
    public string Country { get; private set; } = "ES";
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? SourceFilePath { get; private set; }

    private IssuedInvoice() { } // EF Core

    public IssuedInvoice(
        string series,
        string invoiceNumber,
        DateTime issueDate,
        decimal taxableBase,
        decimal taxAmount,
        decimal totalAmount,
        string clientId,
        string clientTaxId,
        string clientName,
        string address,
        string city,
        string postalCode,
        string province,
        string country = "ES",
        string? sourceFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("El número de factura no puede estar vacío.", nameof(invoiceNumber));
        
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("El nombre del cliente no puede estar vacío.", nameof(clientName));

        if (totalAmount < 0)
            throw new ArgumentException("El total no puede ser negativo.", nameof(totalAmount));

        Id = Guid.NewGuid();
        Series = series ?? string.Empty;
        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        TaxableBase = new Money(taxableBase, "EUR");
        TaxAmount = new Money(taxAmount, "EUR");
        TotalAmount = new Money(totalAmount, "EUR");
        ClientId = clientId ?? string.Empty;
        ClientTaxId = clientTaxId ?? string.Empty;
        ClientName = clientName;
        Address = address ?? string.Empty;
        City = city ?? string.Empty;
        PostalCode = postalCode ?? string.Empty;
        Province = province ?? string.Empty;
        Country = country;
        SourceFilePath = sourceFilePath;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateFromExcel(
        string series,
        DateTime issueDate,
        decimal taxableBase,
        decimal taxAmount,
        decimal totalAmount,
        string clientId,
        string clientTaxId,
        string clientName,
        string address,
        string city,
        string postalCode,
        string province,
        string country)
    {
        Series = series ?? string.Empty;
        IssueDate = issueDate;
        TaxableBase = new Money(taxableBase, "EUR");
        TaxAmount = new Money(taxAmount, "EUR");
        TotalAmount = new Money(totalAmount, "EUR");
        ClientId = clientId ?? string.Empty;
        ClientTaxId = clientTaxId ?? string.Empty;
        ClientName = clientName;
        Address = address ?? string.Empty;
        City = city ?? string.Empty;
        PostalCode = postalCode ?? string.Empty;
        Province = province ?? string.Empty;
        Country = country ?? "ES";
        UpdatedAt = DateTime.UtcNow;
    }
}

