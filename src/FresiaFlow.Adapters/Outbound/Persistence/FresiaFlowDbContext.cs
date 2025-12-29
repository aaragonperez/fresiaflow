using FresiaFlow.Domain.Invoices;
using FresiaFlow.Domain.Banking;
using FresiaFlow.Domain.Tasks;
using FresiaFlow.Domain.Reconciliation;
using FresiaFlow.Domain.InvoicesReceived;
using FresiaFlow.Domain.Sync;
using FresiaFlow.Domain.Accounting;
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
    public DbSet<IssuedInvoice> IssuedInvoices { get; set; } = null!;
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<BankTransaction> BankTransactions { get; set; } = null!;
    public DbSet<TaskItem> Tasks { get; set; } = null!;
    public DbSet<ReconciliationCandidate> ReconciliationCandidates { get; set; } = null!;
    public DbSet<InvoiceReceived> InvoicesReceived { get; set; } = null!;
    public DbSet<InvoiceReceivedLine> InvoiceReceivedLines { get; set; } = null!;
    public DbSet<InvoiceReceivedPayment> InvoiceReceivedPayments { get; set; } = null!;
    public DbSet<SyncedFile> SyncedFiles { get; set; } = null!;
    public DbSet<OneDriveSyncConfig> OneDriveSyncConfigs { get; set; } = null!;
    public DbSet<InvoiceSourceConfig> InvoiceSourceConfigs { get; set; } = null!;
    public DbSet<InvoiceProcessingSnapshot> InvoiceProcessingSnapshots { get; set; } = null!;
    public DbSet<AccountingEntry> AccountingEntries { get; set; } = null!;
    public DbSet<AccountingEntryLine> AccountingEntryLines { get; set; } = null!;
    public DbSet<AccountingAccount> AccountingAccounts { get; set; } = null!;

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

        // Configuración de IssuedInvoice
        modelBuilder.Entity<IssuedInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Series).HasMaxLength(50);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientTaxId).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(2).HasDefaultValue("ES");
            entity.HasIndex(e => new { e.Series, e.InvoiceNumber }).IsUnique();
        });

        // Configuración de Money para IssuedInvoice
        modelBuilder.Entity<IssuedInvoice>(entity =>
        {
            entity.OwnsOne(e => e.TaxableBase, money =>
            {
                money.Property(m => m.Value).HasColumnName("TaxableBase").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("TaxableBaseCurrency").HasMaxLength(3);
            });
            entity.OwnsOne(e => e.TaxAmount, money =>
            {
                money.Property(m => m.Value).HasColumnName("TaxAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("TaxAmountCurrency").HasMaxLength(3);
            });
            entity.OwnsOne(e => e.TotalAmount, money =>
            {
                money.Property(m => m.Value).HasColumnName("TotalAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("TotalAmountCurrency").HasMaxLength(3);
            });
        });

        // Configuración de SyncedFile
        modelBuilder.Entity<SyncedFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.FileHash).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => new { e.Source, e.ExternalId }).IsUnique();
            entity.HasIndex(e => e.FileHash);
        });

        // Configuración de OneDriveSyncConfig
        modelBuilder.Entity<OneDriveSyncConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).HasMaxLength(100);
            entity.Property(e => e.ClientId).HasMaxLength(100);
            entity.Property(e => e.ClientSecret).HasMaxLength(500);
            entity.Property(e => e.FolderPath).HasMaxLength(2000);
            entity.Property(e => e.DriveId).HasMaxLength(500);
            entity.Property(e => e.LastSyncError).HasMaxLength(2000);
        });

        // Configuración de InvoiceSourceConfig
        modelBuilder.Entity<InvoiceSourceConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ConfigJson).IsRequired();
            entity.Property(e => e.LastSyncError).HasMaxLength(2000);
            entity.HasIndex(e => new { e.SourceType, e.Name });
        });

        // Configuración de AccountingAccount
        modelBuilder.Entity<AccountingAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Configuración de AccountingEntry
        modelBuilder.Entity<AccountingEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.EntryNumber).IsRequired(false);
            entity.Property(e => e.EntryYear).IsRequired();
            entity.HasIndex(e => e.EntryDate);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => new { e.Status, e.Source });
            entity.HasIndex(e => new { e.EntryYear, e.EntryNumber }); // Índice para búsqueda rápida de números
            
            // Relación con líneas
            entity.HasMany(e => e.Lines)
                  .WithOne()
                  .HasForeignKey(l => l.AccountingEntryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de AccountingEntryLine
        modelBuilder.Entity<AccountingEntryLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Configuración de Money como value object
            entity.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.Value).HasColumnName("Amount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            
            // Relación con cuenta contable
            entity.HasOne<AccountingAccount>()
                  .WithMany()
                  .HasForeignKey(l => l.AccountingAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

