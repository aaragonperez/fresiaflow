using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FresiaFlow.Adapters.Outbound.Persistence.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad InvoiceReceivedPayment.
/// </summary>
public class InvoiceReceivedPaymentConfiguration : IEntityTypeConfiguration<InvoiceReceivedPayment>
{
    public void Configure(EntityTypeBuilder<InvoiceReceivedPayment> builder)
    {
        builder.ToTable("InvoiceReceivedPayments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.InvoiceReceivedId)
            .IsRequired();

        builder.Property(p => p.BankTransactionId)
            .IsRequired();

        builder.Property(p => p.PaymentDate)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Configurar Money como objeto de valor (Owned Type)
        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Relación con InvoiceReceived
        builder.HasOne(p => p.InvoiceReceived)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceReceivedId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices para optimizar consultas
        builder.HasIndex(p => p.InvoiceReceivedId);
        builder.HasIndex(p => p.BankTransactionId);
        builder.HasIndex(p => p.PaymentDate);
    }
}

