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

        // Índice no único para búsquedas eficientes (permite duplicados para detectar rectificaciones)
        builder.HasIndex(i => i.InvoiceNumber);

        builder.Property(i => i.SupplierName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.SupplierTaxId)
            .HasMaxLength(50);

        builder.Property(i => i.SupplierAddress)
            .HasMaxLength(500);

        builder.Property(i => i.IssueDate)
            .IsRequired();

        builder.Property(i => i.ReceivedDate)
            .IsRequired();

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
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("SubtotalCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(i => i.TaxRate)
            .HasPrecision(5, 2);

        // Configurar IRPF (retención)
        builder.OwnsOne(i => i.IrpfAmount, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("IrpfAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("IrpfCurrency")
                .HasMaxLength(3);
        });

        builder.Property(i => i.IrpfRate)
            .HasPrecision(5, 2);

        builder.Property(i => i.PaymentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Origin)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.OriginalFilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.ProcessedFilePath)
            .HasMaxLength(500);

        builder.Property(i => i.ExtractionConfidence)
            .HasPrecision(3, 2);

        builder.Property(i => i.Notes)
            .HasMaxLength(1000);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired();

        // Relación con líneas
        builder.HasMany(i => i.Lines)
            .WithOne(l => l.InvoiceReceived)
            .HasForeignKey(l => l.InvoiceReceivedId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación con pagos (configurada en InvoiceReceivedPaymentConfiguration)
    }
}
