using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateOneDriveToInvoiceSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrar datos de OneDriveSyncConfig a InvoiceSourceConfig
            migrationBuilder.Sql(@"
                INSERT INTO ""InvoiceSourceConfigs"" (
                    ""Id"", 
                    ""SourceType"", 
                    ""Name"", 
                    ""ConfigJson"", 
                    ""Enabled"", 
                    ""LastSyncAt"", 
                    ""LastSyncError"", 
                    ""TotalFilesSynced"", 
                    ""CreatedAt"", 
                    ""UpdatedAt""
                )
                SELECT 
                    ""Id"",
                    3 as ""SourceType"", -- InvoiceSourceType.OneDrive = 3
                    'OneDrive Sync' as ""Name"",
                    jsonb_build_object(
                        'tenantId', ""TenantId"",
                        'clientId', ""ClientId"",
                        'clientSecret', ""ClientSecret"",
                        'folderPath', ""FolderPath"",
                        'driveId', ""DriveId"",
                        'syncIntervalMinutes', ""SyncIntervalMinutes""
                    )::text as ""ConfigJson"",
                    ""Enabled"",
                    ""LastSyncAt"",
                    ""LastSyncError"",
                    ""TotalFilesSynced"",
                    ""CreatedAt"",
                    ""UpdatedAt""
                FROM ""OneDriveSyncConfigs""
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""InvoiceSourceConfigs"" 
                    WHERE ""InvoiceSourceConfigs"".""SourceType"" = 3
                );
            ");

            // Actualizar SyncedFiles que tienen Source = 'OneDrive' para usar el nuevo formato
            migrationBuilder.Sql(@"
                UPDATE ""SyncedFiles""
                SET ""Source"" = 'OneDrive-' || (
                    SELECT ""Id""::text 
                    FROM ""InvoiceSourceConfigs"" 
                    WHERE ""SourceType"" = 3 
                    LIMIT 1
                )
                WHERE ""Source"" = 'OneDrive'
                  AND EXISTS (
                      SELECT 1 FROM ""InvoiceSourceConfigs"" 
                      WHERE ""SourceType"" = 3
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir cambios en SyncedFiles
            migrationBuilder.Sql(@"
                UPDATE ""SyncedFiles""
                SET ""Source"" = 'OneDrive'
                WHERE ""Source"" LIKE 'OneDrive-%'
                  AND EXISTS (
                      SELECT 1 FROM ""OneDriveSyncConfigs""
                  );
            ");

            // Eliminar las fuentes de OneDrive migradas
            migrationBuilder.Sql(@"
                DELETE FROM ""InvoiceSourceConfigs""
                WHERE ""SourceType"" = 3;
            ");
        }
    }
}
