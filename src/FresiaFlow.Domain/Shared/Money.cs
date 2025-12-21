namespace FresiaFlow.Domain.Shared;

/// <summary>
/// Value Object que representa una cantidad monetaria.
/// </summary>
public class Money
{
    public decimal Value { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    private Money() { } // EF Core

    public Money(decimal value, string currency = "EUR")
    {
        if (value < 0)
            throw new ArgumentException("El valor no puede ser negativo.", nameof(value));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("La moneda no puede estar vacía.", nameof(currency));

        Value = value;
        Currency = currency;
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("No se pueden sumar cantidades de diferentes monedas.");

        return new Money(left.Value + right.Value, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("No se pueden restar cantidades de diferentes monedas.");

        return new Money(left.Value - right.Value, left.Currency);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Money other)
        {
            return Value == other.Value && Currency == other.Currency;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Currency);
    }

    public override string ToString()
    {
        return $"{Value:C} {Currency}";
    }
}

