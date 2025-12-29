using FresiaFlow.Domain.Shared;
using FluentAssertions;

namespace FresiaFlow.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(1000m, "EUR");

        // Assert
        money.Value.Should().Be(1000m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_WithDefaultCurrency_ShouldUseEUR()
    {
        // Arrange & Act
        var money = new Money(1000m);

        // Assert
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_WithNegativeValue_ShouldAllowNegativeAmounts()
    {
        var money = new Money(-100m, "EUR");

        money.Value.Should().Be(-100m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_WithEmptyCurrency_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new Money(100m, "");
        act.Should().Throw<ArgumentException>()
            .WithMessage("La moneda no puede estar vacía.*");
    }

    [Fact]
    public void OperatorPlus_WithSameCurrency_ShouldAddValues()
    {
        // Arrange
        var money1 = new Money(500m, "EUR");
        var money2 = new Money(300m, "EUR");

        // Act
        var result = money1 + money2;

        // Assert
        result.Value.Should().Be(800m);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public void OperatorPlus_WithDifferentCurrencies_ShouldThrowException()
    {
        // Arrange
        var money1 = new Money(500m, "EUR");
        var money2 = new Money(300m, "USD");

        // Act & Assert
        var act = () => money1 + money2;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No se pueden sumar cantidades de diferentes monedas.*");
    }

    [Fact]
    public void OperatorMinus_WithSameCurrency_ShouldSubtractValues()
    {
        // Arrange
        var money1 = new Money(500m, "EUR");
        var money2 = new Money(300m, "EUR");

        // Act
        var result = money1 - money2;

        // Assert
        result.Value.Should().Be(200m);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public void OperatorMinus_WithDifferentCurrencies_ShouldThrowException()
    {
        // Arrange
        var money1 = new Money(500m, "EUR");
        var money2 = new Money(300m, "USD");

        // Act & Assert
        var act = () => money1 - money2;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No se pueden restar cantidades de diferentes monedas.*");
    }

    [Fact]
    public void Equals_WithSameValueAndCurrency_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(1000m, "EUR");
        var money2 = new Money(1000m, "EUR");

        // Act & Assert
        money1.Equals(money2).Should().BeTrue();
        (money1 == money2).Should().BeFalse(); // No hay operador == definido, pero Equals funciona
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(1000m, "EUR");
        var money2 = new Money(2000m, "EUR");

        // Act & Assert
        money1.Equals(money2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentCurrency_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(1000m, "EUR");
        var money2 = new Money(1000m, "USD");

        // Act & Assert
        money1.Equals(money2).Should().BeFalse();
    }
}

