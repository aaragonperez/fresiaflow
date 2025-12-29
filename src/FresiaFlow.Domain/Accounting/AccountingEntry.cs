using FresiaFlow.Domain.Shared;

namespace FresiaFlow.Domain.Accounting;

/// <summary>
/// Asiento contable.
/// Representa un asiento contable con sus líneas (debe/haber).
/// </summary>
public class AccountingEntry
{
    public Guid Id { get; private set; }
    public int? EntryNumber { get; private set; } // Número correlativo del asiento (por año)
    public int EntryYear { get; private set; } // Año del asiento para la numeración correlativa
    public DateTime EntryDate { get; private set; } // Fecha del asiento
    public string Description { get; private set; } = string.Empty; // Descripción del asiento
    public string? Reference { get; private set; } // Referencia externa (ej: número de factura)
    public Guid? InvoiceId { get; private set; } // Referencia a la factura que generó este asiento
    public EntrySource Source { get; private set; } // Origen del asiento (Automático, Manual)
    public EntryStatus Status { get; private set; } // Estado (Draft, Posted, Reversed)
    public bool IsReversed { get; private set; } // Si está anulado
    public Guid? ReversedByEntryId { get; private set; } // ID del asiento que lo anuló
    public string? Notes { get; private set; } // Notas adicionales
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<AccountingEntryLine> _lines = new();
    public IReadOnlyCollection<AccountingEntryLine> Lines => _lines.AsReadOnly();

    private AccountingEntry() { }

    public AccountingEntry(
        DateTime entryDate,
        string description,
        EntrySource source,
        string? reference = null,
        Guid? invoiceId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede estar vacía.", nameof(description));

        Id = Guid.NewGuid();
        EntryDate = entryDate;
        EntryYear = entryDate.Year;
        EntryNumber = null; // Se asignará cuando se contabilice el asiento
        Description = description;
        Reference = reference;
        InvoiceId = invoiceId;
        Source = source;
        Status = EntryStatus.Draft;
        IsReversed = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Agrega una línea al asiento (debe o haber).
    /// </summary>
    public void AddLine(AccountingEntryLine line)
    {
        if (line == null)
            throw new ArgumentNullException(nameof(line));

        if (line.AccountingEntryId != Id)
            throw new ArgumentException("La línea no pertenece a este asiento.", nameof(line));

        if (Status == EntryStatus.Posted)
            throw new InvalidOperationException("No se pueden modificar asientos ya contabilizados.");

        _lines.Add(line);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reemplaza todas las líneas del asiento.
    /// </summary>
    public void ReplaceLines(IEnumerable<AccountingEntryLine> lines)
    {
        if (lines == null)
            throw new ArgumentNullException(nameof(lines));

        if (Status == EntryStatus.Posted)
            throw new InvalidOperationException("No se pueden modificar asientos ya contabilizados.");

        _lines.Clear();
        foreach (var line in lines)
        {
            if (line.AccountingEntryId != Id)
                throw new ArgumentException("La línea no pertenece a este asiento.", nameof(lines));
            
            _lines.Add(line);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Valida que el asiento esté balanceado (suma de debe = suma de haber).
    /// </summary>
    public bool IsBalanced()
    {
        if (_lines.Count == 0)
            return false;

        var totalDebit = _lines.Where(l => l.Side == EntrySide.Debit)
            .Sum(l => l.Amount.Value);
        
        var totalCredit = _lines.Where(l => l.Side == EntrySide.Credit)
            .Sum(l => l.Amount.Value);

        return Math.Abs(totalDebit - totalCredit) < 0.01m; // Tolerancia para decimales
    }

    /// <summary>
    /// Asigna un número de asiento (puede hacerse antes de contabilizar).
    /// </summary>
    public void AssignEntryNumber(int entryNumber)
    {
        if (entryNumber <= 0)
            throw new ArgumentException("El número de asiento debe ser mayor que cero.", nameof(entryNumber));

        if (EntryNumber.HasValue)
            throw new InvalidOperationException("El asiento ya tiene un número asignado.");

        EntryNumber = entryNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Contabiliza el asiento (cambia estado a Posted).
    /// </summary>
    public void Post(int entryNumber)
    {
        if (!IsBalanced())
            throw new InvalidOperationException("El asiento no está balanceado. Debe = Haber.");

        if (Status == EntryStatus.Posted)
            throw new InvalidOperationException("El asiento ya está contabilizado.");

        if (entryNumber <= 0)
            throw new ArgumentException("El número de asiento debe ser mayor que cero.", nameof(entryNumber));

        // Si ya tiene número, verificar que coincida
        if (EntryNumber.HasValue && EntryNumber.Value != entryNumber)
            throw new InvalidOperationException($"El asiento ya tiene el número {EntryNumber.Value} asignado.");

        EntryNumber = entryNumber;
        Status = EntryStatus.Posted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Anula el asiento creando un asiento de reversión.
    /// </summary>
    public void Reverse(Guid reversedByEntryId)
    {
        if (Status != EntryStatus.Posted)
            throw new InvalidOperationException("Solo se pueden anular asientos contabilizados.");

        if (IsReversed)
            throw new InvalidOperationException("El asiento ya está anulado.");

        IsReversed = true;
        ReversedByEntryId = reversedByEntryId;
        Status = EntryStatus.Reversed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la descripción del asiento.
    /// </summary>
    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede estar vacía.", nameof(description));

        if (Status == EntryStatus.Posted && Source == EntrySource.Automatic)
            throw new InvalidOperationException("No se puede modificar la descripción de asientos automáticos contabilizados.");

        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la fecha del asiento (solo para asientos manuales).
    /// </summary>
    public void UpdateEntryDate(DateTime entryDate)
    {
        if (Status == EntryStatus.Posted && Source == EntrySource.Automatic)
            throw new InvalidOperationException("No se puede modificar la fecha de asientos automáticos contabilizados.");

        EntryDate = entryDate;
        EntryYear = entryDate.Year;
        // Si el año cambia y el asiento ya está contabilizado, se debe anular y recrear
        // Por ahora, solo actualizamos el año
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza las notas del asiento.
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtiene el total del debe.
    /// </summary>
    public Money GetTotalDebit()
    {
        if (_lines.Count == 0)
            return new Money(0, "EUR");

        var firstLine = _lines.First();
        var total = _lines.Where(l => l.Side == EntrySide.Debit)
            .Sum(l => l.Amount.Value);
        
        return new Money(total, firstLine.Amount.Currency);
    }

    /// <summary>
    /// Obtiene el total del haber.
    /// </summary>
    public Money GetTotalCredit()
    {
        if (_lines.Count == 0)
            return new Money(0, "EUR");

        var firstLine = _lines.First();
        var total = _lines.Where(l => l.Side == EntrySide.Credit)
            .Sum(l => l.Amount.Value);
        
        return new Money(total, firstLine.Amount.Currency);
    }
}

/// <summary>
/// Origen del asiento contable.
/// </summary>
public enum EntrySource
{
    Automatic = 1, // Generado automáticamente desde facturas
    Manual = 2     // Creado manualmente por el usuario
}

/// <summary>
/// Estado del asiento contable.
/// </summary>
public enum EntryStatus
{
    Draft = 1,    // Borrador (se puede modificar)
    Posted = 2,   // Contabilizado (no se puede modificar)
    Reversed = 3  // Anulado
}

