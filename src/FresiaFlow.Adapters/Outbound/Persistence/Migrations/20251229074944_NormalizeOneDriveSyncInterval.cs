using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FresiaFlow.Adapters.Outbound.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeOneDriveSyncInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalizar syncIntervalMinutes en todas las configuraciones de OneDrive
            // 1. Asegurar que todos los registros tengan syncIntervalMinutes
            // 2. Corregir valores inválidos (null, < 1, > 1440, o = 60 que parece ser un valor por defecto incorrecto)
            // 3. Establecer 15 como valor por defecto si no existe o es inválido
            
            migrationBuilder.Sql(@"
UPDATE ""InvoiceSourceConfigs""
SET ""ConfigJson"" = 
    CASE
        WHEN jsonb_exists(""ConfigJson""::jsonb, 'syncIntervalMinutes') = false 
             OR (""ConfigJson""::jsonb->>'syncIntervalMinutes') IS NULL
             OR (""ConfigJson""::jsonb->>'syncIntervalMinutes') = 'null'
        THEN jsonb_set(
            ""ConfigJson""::jsonb,
            '{syncIntervalMinutes}',
            '15'::jsonb,
            true
        )::text
        WHEN ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) = 60
        THEN jsonb_set(
            ""ConfigJson""::jsonb,
            '{syncIntervalMinutes}',
            '15'::jsonb,
            true
        )::text
        WHEN ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) < 1 
             OR ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) > 1440
        THEN jsonb_set(
            ""ConfigJson""::jsonb,
            '{syncIntervalMinutes}',
            '15'::jsonb,
            true
        )::text
        ELSE ""ConfigJson""
    END,
    ""UpdatedAt"" = NOW()
WHERE ""SourceType"" = 3
  AND (
      jsonb_exists(""ConfigJson""::jsonb, 'syncIntervalMinutes') = false
      OR (""ConfigJson""::jsonb->>'syncIntervalMinutes') IS NULL
      OR (""ConfigJson""::jsonb->>'syncIntervalMinutes') = 'null'
      OR ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) = 60
      OR ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) < 1
      OR ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) > 1440
  );
");

            // Log de resumen (opcional, para verificación)
            migrationBuilder.Sql(@"
DO $$
DECLARE
    total_count INTEGER;
    fixed_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO total_count
    FROM ""InvoiceSourceConfigs""
    WHERE ""SourceType"" = 3;
    
    SELECT COUNT(*) INTO fixed_count
    FROM ""InvoiceSourceConfigs""
    WHERE ""SourceType"" = 3
      AND ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) BETWEEN 1 AND 1440
      AND ((""ConfigJson""::jsonb->>'syncIntervalMinutes')::int) != 60;
    
    RAISE NOTICE 'OneDrive sources: Total: %, Fixed: %', total_count, fixed_count;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
