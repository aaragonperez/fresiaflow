using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Banking;
using FresiaFlow.Domain.Reconciliation;

namespace FresiaFlow.Application.Policies;

/// <summary>
/// Política de conciliación bancaria.
/// Define reglas para conciliar automáticamente facturas con transacciones.
/// </summary>
public static class ReconciliationPolicy
{
    /// <summary>
    /// Encuentra candidatos para conciliación automática.
    /// </summary>
    public static List<ReconciliationCandidate> FindCandidates(
        List<Invoice> invoices,
        List<BankTransaction> transactions)
    {
        var candidates = new List<ReconciliationCandidate>();

        foreach (var invoice in invoices.Where(i => i.Status != InvoiceStatus.Paid))
        {
            foreach (var transaction in transactions.Where(t => !t.IsReconciled))
            {
                if (invoice.CanBeReconciledWith(transaction.Amount.Value, transaction.TransactionDate))
                {
                    var candidate = ReconciliationCandidate.Create(invoice, transaction);
                    if (candidate.MatchScore >= 0.8m) // Umbral mínimo
                    {
                        candidates.Add(candidate);
                    }
                }
            }
        }

        return candidates.OrderByDescending(c => c.MatchScore).ToList();
    }

    /// <summary>
    /// Determina si una conciliación puede ser automática o requiere revisión.
    /// </summary>
    public static bool CanAutoReconcile(ReconciliationCandidate candidate)
    {
        // Score >= 0.95: conciliación automática
        // Score < 0.95: requiere revisión manual
        return candidate.MatchScore >= 0.95m;
    }
}

