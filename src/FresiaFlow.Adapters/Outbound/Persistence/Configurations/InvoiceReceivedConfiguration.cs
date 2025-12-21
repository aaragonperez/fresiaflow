using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FresiaFlow.Adapters.Outbound.Persistence.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad InvoiceReceived.
/// </summary>
public class InvoiceReceivedConfiguration : IEntityTypeConfiguration<InvoiceReceived>
{
    public void Configure(EntityTypeBuilder<InvoiceReceived> builder)
    {
        builder.ToTable("InvoicesReceived");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        builder.Property(i => i.SupplierName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.SupplierTaxId)
            .HasMaxLength(50);

        builder.Property(i => i.IssueDate)
            .IsRequired();

        builder.Property(i => i.DueDate);

        // Configurar Money como objeto de valor (Owned Type)
        builder.OwnsOne(i => i.TotalAmount, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("TotalAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(i => i.TaxAmount, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("TaxAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("TaxCurrency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(i => i.SubtotalAmount, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("SubtotalAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("SubtotalCurrency")
                .HasMaxLength(3);
        });

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(i => i.OriginalFilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.ProcessedFilePath)
            .HasMaxLength(500);

        builder.Property(i => i.ProcessedAt)
            .IsRequired();

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Notes)
            .HasMaxLength(1000);

        // Relación con líneas
        builder.HasMany(i => i.Lines)
            .WithOne(l => l.InvoiceReceived)
            .HasForeignKey(l => l.InvoiceReceivedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

