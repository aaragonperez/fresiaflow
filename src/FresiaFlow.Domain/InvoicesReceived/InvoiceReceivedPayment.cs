using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.InvoicesReceived;

/// <summary>
/// Relación entre una factura recibida y un movimiento bancario.
/// Representa que una factura fue pagada (parcial o totalmente) mediante una transacción bancaria.
/// </summary>
public class InvoiceReceivedPayment
{
    public Guid Id { get; private set; }
    public Guid InvoiceReceivedId { get; private set; }
    public Guid BankTransactionId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTime PaymentDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navegación
    public InvoiceReceived InvoiceReceived { get; private set; } = null!;

    // Constructor privado para EF Core
    private InvoiceReceivedPayment()
    {
        Amount = new Money(0, "EUR");
    }

    public InvoiceReceivedPayment(
        Guid invoiceReceivedId,
        Guid bankTransactionId,
        Money amount,
        DateTime paymentDate)
    {
        if (amount.Value <= 0)
            throw new ArgumentException("El importe del pago debe ser positivo.", nameof(amount));

        Id = Guid.NewGuid();
        InvoiceReceivedId = invoiceReceivedId;
        BankTransactionId = bankTransactionId;
        Amount = amount;
        PaymentDate = paymentDate;
        CreatedAt = DateTime.UtcNow;
    }
}

