using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingInvoicesData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Actualizar datos existentes: establecer CreatedAt y ReceivedDate basados en UpdatedAt (anteriormente ProcessedAt)
            migrationBuilder.Sql(@"
                UPDATE ""InvoicesReceived""
                SET 
                    ""CreatedAt"" = COALESCE(""UpdatedAt"", NOW()),
                    ""ReceivedDate"" = COALESCE(""UpdatedAt"", NOW()),
                    ""Origin"" = 'ManualUpload',
                    ""PaymentType"" = 'Cash'
                WHERE ""CreatedAt"" = '0001-01-01 00:00:00+00'::timestamp with time zone
                   OR ""ReceivedDate"" = '0001-01-01 00:00:00+00'::timestamp with time zone;
            ");

            // Eliminar la columna shadow InvoiceReceivedId1 si existe
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'InvoiceReceivedPayments' 
                        AND column_name = 'InvoiceReceivedId1'
                    ) THEN
                        ALTER TABLE ""InvoiceReceivedPayments"" DROP COLUMN ""InvoiceReceivedId1"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No hay reversión necesaria
        }
    }
}
