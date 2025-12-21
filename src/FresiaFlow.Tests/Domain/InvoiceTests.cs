using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Shared;
using FluentAssertions;

namespace FresiaFlow.Tests.Domain;

public class InvoiceTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateInvoice()
    {
        // Arrange
        var invoiceNumber = "INV-001";
        var issueDate = new DateTime(2024, 1, 15);
        var dueDate = new DateTime(2024, 2, 15);
        var amount = new Money(1000m, "EUR");
        var supplierName = "Proveedor Test";

        // Act
        var invoice = new Invoice(invoiceNumber, issueDate, dueDate, amount, supplierName);

        // Assert
        invoice.Should().NotBeNull();
        invoice.InvoiceNumber.Should().Be(invoiceNumber);
        invoice.IssueDate.Should().Be(issueDate);
        invoice.DueDate.Should().Be(dueDate);
        invoice.Amount.Should().Be(amount);
        invoice.SupplierName.Should().Be(supplierName);
        invoice.Status.Should().Be(InvoiceStatus.Pending);
        invoice.Id.Should().NotBeEmpty();
        invoice.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithNullDueDate_ShouldCreateInvoice()
    {
        // Arrange
        var invoiceNumber = "INV-002";
        var issueDate = new DateTime(2024, 1, 15);
        var amount = new Money(500m, "EUR");
        var supplierName = "Proveedor Test";

        // Act
        var invoice = new Invoice(invoiceNumber, issueDate, null, amount, supplierName);

        // Assert
        invoice.DueDate.Should().BeNull();
        invoice.Status.Should().Be(InvoiceStatus.Pending);
    }

    [Fact]
    public void MarkAsPaid_WhenPending_ShouldUpdateStatus()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        var transactionId = Guid.NewGuid();

        // Act
        invoice.MarkAsPaid(transactionId);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.ReconciledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        invoice.ReconciledWithTransactionId.Should().Be(transactionId);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldThrowException()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        var transactionId = Guid.NewGuid();
        invoice.MarkAsPaid(transactionId);

        // Act & Assert
        var act = () => invoice.MarkAsPaid(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("La factura ya está marcada como pagada.");
    }

    [Fact]
    public void MarkAsOverdue_WhenDueDatePassed_ShouldUpdateStatus()
    {
        // Arrange
        var invoice = new Invoice(
            "INV-003",
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 10), // Fecha vencida
            new Money(1000m, "EUR"),
            "Proveedor Test");

        // Act
        invoice.MarkAsOverdue();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Overdue);
    }

    [Fact]
    public void MarkAsOverdue_WhenAlreadyPaid_ShouldNotChangeStatus()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        invoice.MarkAsPaid(Guid.NewGuid());

        // Act
        invoice.MarkAsOverdue();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void MarkAsOverdue_WhenDueDateNotPassed_ShouldNotChangeStatus()
    {
        // Arrange
        var invoice = new Invoice(
            "INV-004",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(10), // Fecha futura
            new Money(1000m, "EUR"),
            "Proveedor Test");

        // Act
        invoice.MarkAsOverdue();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Pending);
    }

    [Fact]
    public void CanBeReconciledWith_WhenValidTransaction_ShouldReturnTrue()
    {
        // Arrange
        var invoice = new Invoice(
            "INV-005",
            new DateTime(2024, 1, 15),
            new DateTime(2024, 2, 15),
            new Money(1000m, "EUR"),
            "Proveedor Test");
        var transactionAmount = 1000m; // Monto exacto
        var transactionDate = new DateTime(2024, 2, 15); // Fecha exacta

        // Act
        var result = invoice.CanBeReconciledWith(transactionAmount, transactionDate);

        // Assert
        result.Should().BeTrue();
    }

    private static Invoice CreateTestInvoice()
    {
        return new Invoice(
            "INV-TEST",
            new DateTime(2024, 1, 15),
            new DateTime(2024, 2, 15),
            new Money(1000m, "EUR"),
            "Proveedor Test");
    }
}

