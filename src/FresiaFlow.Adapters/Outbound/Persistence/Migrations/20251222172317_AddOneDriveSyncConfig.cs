using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOneDriveSyncConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar la columna shadow InvoiceReceivedId1 si existe (puede haber sido eliminada por migración anterior)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'InvoiceReceivedPayments' 
                        AND column_name = 'InvoiceReceivedId1'
                    ) THEN
                        -- Primero eliminar la foreign key si existe
                        IF EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_InvoiceReceivedPayments_InvoicesReceived_InvoiceReceivedId1'
                        ) THEN
                            ALTER TABLE ""InvoiceReceivedPayments"" 
                            DROP CONSTRAINT ""FK_InvoiceReceivedPayments_InvoicesReceived_InvoiceReceivedId1"";
                        END IF;
                        
                        -- Eliminar el índice si existe
                        IF EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE indexname = 'IX_InvoiceReceivedPayments_InvoiceReceivedId1'
                        ) THEN
                            DROP INDEX IF EXISTS ""IX_InvoiceReceivedPayments_InvoiceReceivedId1"";
                        END IF;
                        
                        -- Finalmente eliminar la columna
                        ALTER TABLE ""InvoiceReceivedPayments"" DROP COLUMN ""InvoiceReceivedId1"";
                    END IF;
                END $$;
            ");

            migrationBuilder.CreateTable(
                name: "OneDriveSyncConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FolderPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DriveId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TotalFilesSynced = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneDriveSyncConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncedFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncedFiles_FileHash",
                table: "SyncedFiles",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedFiles_Source_ExternalId",
                table: "SyncedFiles",
                columns: new[] { "Source", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneDriveSyncConfigs");

            migrationBuilder.DropTable(
                name: "SyncedFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceReceivedId1",
                table: "InvoiceReceivedPayments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReceivedPayments_InvoiceReceivedId1",
                table: "InvoiceReceivedPayments",
                column: "InvoiceReceivedId1");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceReceivedPayments_InvoicesReceived_InvoiceReceivedId1",
                table: "InvoiceReceivedPayments",
                column: "InvoiceReceivedId1",
                principalTable: "InvoicesReceived",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
