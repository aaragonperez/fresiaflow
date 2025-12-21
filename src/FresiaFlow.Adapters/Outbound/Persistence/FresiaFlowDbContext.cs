using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Banking;
using FresiaFlow.Domain.Tasks;
using FresiaFlow.Domain.Reconciliation;
using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// DbContext de Entity Framework Core para FresiaFlow.
/// Aislado en la capa de adaptadores.
/// </summary>
public class FresiaFlowDbContext : DbContext
{
    public FresiaFlowDbContext(DbContextOptions<FresiaFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<BankTransaction> BankTransactions { get; set; } = null!;
    public DbSet<TaskItem> Tasks { get; set; } = null!;
    public DbSet<ReconciliationCandidate> ReconciliationCandidates { get; set; } = null!;
    public DbSet<InvoiceReceived> InvoicesReceived { get; set; } = null!;
    public DbSet<InvoiceReceivedLine> InvoiceReceivedLines { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configuraciones específicas
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FresiaFlowDbContext).Assembly);

        // Configuración de Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
        });

        // Configuración de BankAccount
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BankName).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.Transactions)
                  .WithOne()
                  .HasForeignKey(t => t.BankAccountId);
        });

        // Configuración de BankTransaction
        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.ExternalTransactionId);
        });

        // Configuración de TaskItem
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
        });

        // Configuración de ReconciliationCandidate
        modelBuilder.Entity<ReconciliationCandidate>(entity =>
        {
            entity.HasKey(e => new { e.InvoiceId, e.TransactionId });
        });

        // Configuración de Money como value object (owned entity)
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.Value).HasColumnName("Amount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
        });

        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.Value).HasColumnName("Amount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
        });
    }
}

