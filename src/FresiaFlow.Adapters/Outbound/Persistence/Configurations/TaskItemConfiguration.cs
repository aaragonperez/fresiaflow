using FresiaFlow.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FresiaFlow.Adapters.Outbound.Persistence.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad TaskItem.
/// </summary>
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.IsCompleted)
            .IsRequired();

        builder.Property(t => t.IsPinned)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.DueDate);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CompletedAt);

        builder.Property(t => t.RelatedInvoiceId);

        builder.Property(t => t.RelatedTransactionId);

        // Índice para tareas pendientes y fijadas
        builder.HasIndex(t => new { t.IsCompleted, t.IsPinned, t.Priority });
    }
}

