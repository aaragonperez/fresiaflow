namespace FresiaFlow.Domain.Accounting;

/// <summary>
/// Plan de cuentas contable.
/// Representa una cuenta contable del plan general.
/// </summary>
public class AccountingAccount
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty; // Código de cuenta (ej: "600", "400", "472")
    public string Name { get; private set; } = string.Empty; // Nombre de la cuenta
    public AccountType Type { get; private set; } // Tipo de cuenta (Activo, Pasivo, Ingreso, Gasto)
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AccountingAccount() { }

    public AccountingAccount(string code, string name, AccountType type)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código de cuenta no puede estar vacío.", nameof(code));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de cuenta no puede estar vacío.", nameof(name));

        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        Type = type;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de cuenta no puede estar vacío.", nameof(name));
        
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Tipo de cuenta contable según el Plan General Contable español.
/// </summary>
public enum AccountType
{
    Asset = 1,      // Activo (1-5)
    Liability = 2,  // Pasivo (1-5)
    Equity = 3,     // Patrimonio Neto (1-5)
    Income = 4,     // Ingresos (6-7)
    Expense = 5     // Gastos (6-7)
}

