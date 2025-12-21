using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Shared;
using FluentAssertions;

namespace FresiaFlow.Tests.Domain;

public class InvoiceRulesTests
{
    [Fact]
    public void CanReconcile_WithExactMatch_ShouldReturnTrue()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        var transactionAmount = 1000m;
        var transactionDate = new DateTime(2024, 2, 15);

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanReconcile_WithAmountWithinTolerance_ShouldReturnTrue()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        var transactionAmount = 1040m; // +4% (dentro de ±5%)
        var transactionDate = new DateTime(2024, 2, 15);

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanReconcile_WithAmountOutsideTolerance_ShouldReturnFalse()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        var transactionAmount = 1100m; // +10% (fuera de ±5%)
        var transactionDate = new DateTime(2024, 2, 15);

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReconcile_WithDateWithinTolerance_ShouldReturnTrue()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        var transactionAmount = 1000m;
        var transactionDate = new DateTime(2024, 2, 17); // +2 días (dentro de ±3)

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanReconcile_WithDateOutsideTolerance_ShouldReturnFalse()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        var transactionAmount = 1000m;
        var transactionDate = new DateTime(2024, 2, 20); // +5 días (fuera de ±3)

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReconcile_WhenInvoiceIsPaid_ShouldReturnFalse()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        invoice.MarkAsPaid(Guid.NewGuid());
        var transactionAmount = 1000m;
        var transactionDate = new DateTime(2024, 2, 15);

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReconcile_WhenInvoiceIsCancelled_ShouldReturnFalse()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        // Nota: No hay método público para cancelar, pero si existiera, debería retornar false
        // Por ahora, verificamos que una factura pagada no puede reconciliarse
        invoice.MarkAsPaid(Guid.NewGuid());
        var transactionAmount = 1000m;
        var transactionDate = new DateTime(2024, 2, 15);

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateMatchScore_WithPerfectMatch_ShouldReturnOne()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        var transactionAmount = 1000m;
        var transactionDate = new DateTime(2024, 2, 15);

        // Act
        var score = InvoiceRules.CalculateMatchScore(invoice, transactionAmount, transactionDate);

        // Assert
        score.Should().BeApproximately(1.0m, 0.01m);
    }

    [Fact]
    public void CalculateMatchScore_WithPartialMatch_ShouldReturnScoreBetweenZeroAndOne()
    {
        // Arrange
        var invoice = CreateTestInvoice(1000m, new DateTime(2024, 2, 15));
        var transactionAmount = 1050m; // +5%
        var transactionDate = new DateTime(2024, 2, 17); // +2 días

        // Act
        var score = InvoiceRules.CalculateMatchScore(invoice, transactionAmount, transactionDate);

        // Assert
        score.Should().BeGreaterThan(0m);
        score.Should().BeLessThan(1.0m);
    }

    [Fact]
    public void CanReconcile_WithNullDueDate_UsesIssueDate()
    {
        // Arrange
        var invoice = new Invoice(
            "INV-006",
            new DateTime(2024, 1, 15),
            null, // Sin fecha de vencimiento
            new Money(1000m, "EUR"),
            "Proveedor Test");
        var transactionAmount = 1000m;
        var transactionDate = new DateTime(2024, 1, 17); // +2 días desde issue date

        // Act
        var result = InvoiceRules.CanReconcile(invoice, transactionAmount, transactionDate);

        // Assert
        result.Should().BeTrue();
    }

    private static Invoice CreateTestInvoice(decimal amount, DateTime dueDate)
    {
        return new Invoice(
            "INV-TEST",
            new DateTime(2024, 1, 15),
            dueDate,
            new Money(amount, "EUR"),
            "Proveedor Test");
    }
}

