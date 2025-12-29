using FresiaFlow.Domain.InvoicesReceived;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FresiaFlow.Adapters.Outbound.Persistence.Configurations;

public class InvoiceProcessingSnapshotConfiguration : IEntityTypeConfiguration<InvoiceProcessingSnapshot>
{
    public void Configure(EntityTypeBuilder<InvoiceProcessingSnapshot> builder)
    {
        builder.ToTable("InvoiceProcessingSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceFilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.SourceFileHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(x => x.SourceFilePath);
        builder.HasIndex(x => x.SourceFileHash).IsUnique();

        builder.Property(x => x.OcrText)
            .HasColumnType("text");

        builder.Property(x => x.OcrLayoutJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.ClassificationJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.ExtractionJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.ValidationErrors)
            .HasColumnType("text");

        builder.Property(x => x.DocumentType).HasMaxLength(50);
        builder.Property(x => x.DocumentLanguage).HasMaxLength(10);
        builder.Property(x => x.SupplierCandidate).HasMaxLength(250);
        builder.Property(x => x.ExtractionVersion).HasMaxLength(20);
        builder.Property(x => x.ExtractionHash).HasMaxLength(128);
        builder.Property(x => x.FallbackReason).HasMaxLength(500);
    }
}

