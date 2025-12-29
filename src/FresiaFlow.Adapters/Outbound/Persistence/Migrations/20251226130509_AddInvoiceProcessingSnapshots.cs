using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceProcessingSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceProcessingSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceFileHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OcrText = table.Column<string>(type: "text", nullable: true),
                    OcrLayoutJson = table.Column<string>(type: "jsonb", nullable: true),
                    OcrConfidence = table.Column<decimal>(type: "numeric", nullable: true),
                    OcrCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DocumentLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SupplierCandidate = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ClassificationConfidence = table.Column<decimal>(type: "numeric", nullable: true),
                    ClassificationJson = table.Column<string>(type: "jsonb", nullable: true),
                    ClassificationCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtractionJson = table.Column<string>(type: "jsonb", nullable: true),
                    ExtractionVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ExtractionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExtractionConfidence = table.Column<decimal>(type: "numeric", nullable: true),
                    ExtractionCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidationStatus = table.Column<int>(type: "integer", nullable: false),
                    ValidationErrors = table.Column<string>(type: "text", nullable: true),
                    ValidationCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FallbackTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    FallbackReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FallbackCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceProcessingSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceProcessingSnapshots_SourceFileHash",
                table: "InvoiceProcessingSnapshots",
                column: "SourceFileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceProcessingSnapshots_SourceFilePath",
                table: "InvoiceProcessingSnapshots",
                column: "SourceFilePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceProcessingSnapshots");
        }
    }
}
