using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Banking;

namespace FresiaFlow.Domain.Reconciliation;

/// <summary>
/// Representa un candidato para conciliación automática entre factura y transacción.
/// </summary>
public class ReconciliationCandidate
{
    public Guid InvoiceId { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal MatchScore { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private ReconciliationCandidate() { } // EF Core

    public ReconciliationCandidate(
        Guid invoiceId,
        Guid transactionId,
        decimal matchScore,
        string reason)
    {
        InvoiceId = invoiceId;
        TransactionId = transactionId;
        MatchScore = matchScore;
        Reason = reason;
        CreatedAt = DateTime.UtcNow;
    }

    public static ReconciliationCandidate Create(
        Invoice invoice,
        BankTransaction transaction)
    {
        var matchScore = InvoiceRules.CalculateMatchScore(
            invoice,
            transaction.Amount.Value,
            transaction.TransactionDate);

        var reason = $"Coincidencia automática: monto {invoice.Amount.Value:C} vs {transaction.Amount.Value:C}, " +
                     $"fecha {invoice.DueDate?.ToString("yyyy-MM-dd") ?? invoice.IssueDate.ToString("yyyy-MM-dd")} vs {transaction.TransactionDate:yyyy-MM-dd}";

        return new ReconciliationCandidate(
            invoice.Id,
            transaction.Id,
            matchScore,
            reason);
    }
}

