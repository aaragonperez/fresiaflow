using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FresiaFlow.Adapters.Outbound.Persistence.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad InvoiceReceivedLine.
/// </summary>
public class InvoiceReceivedLineConfiguration : IEntityTypeConfiguration<InvoiceReceivedLine>
{
    public void Configure(EntityTypeBuilder<InvoiceReceivedLine> builder)
    {
        builder.ToTable("InvoiceReceivedLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.InvoiceReceivedId)
            .IsRequired();

        builder.Property(l => l.LineNumber)
            .IsRequired();

        builder.Property(l => l.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.Quantity)
            .IsRequired()
            .HasPrecision(18, 4);

        // Configurar Money como objeto de valor (Owned Type)
        builder.OwnsOne(l => l.UnitPrice, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("UnitPrice")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("UnitPriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(l => l.TaxRate)
            .HasPrecision(5, 2);

        builder.OwnsOne(l => l.LineTotal, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("LineTotal")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("LineTotalCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Índice compuesto para optimizar consultas
        builder.HasIndex(l => new { l.InvoiceReceivedId, l.LineNumber });
    }
}

