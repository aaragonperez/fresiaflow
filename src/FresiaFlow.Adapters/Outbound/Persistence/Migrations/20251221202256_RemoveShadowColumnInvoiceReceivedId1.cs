using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowColumnInvoiceReceivedId1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar la columna shadow InvoiceReceivedId1 si existe
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No hay reversión necesaria
        }
    }
}
