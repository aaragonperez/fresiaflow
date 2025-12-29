-- Script para verificar y corregir syncIntervalMinutes en OneDrive sources
-- Este script muestra los valores actuales y permite corregirlos si es necesario

-- 1. Ver valores actuales
SELECT 
    id,
    name,
    "SourceType",
    enabled,
    "ConfigJson",
    CASE 
        WHEN "ConfigJson"::jsonb ? 'syncIntervalMinutes' THEN ("ConfigJson"::jsonb->>'syncIntervalMinutes')::int
        ELSE NULL
    END as sync_interval_minutes
FROM "InvoiceSourceConfigs"
WHERE "SourceType" = 3  -- OneDrive = 3
ORDER BY "UpdatedAt" DESC;

-- 2. Si necesitas corregir un valor específico (ejemplo: cambiar 60 a 1440)
-- Descomenta y ajusta el ID y el valor según necesites:
/*
UPDATE "InvoiceSourceConfigs"
SET "ConfigJson" = jsonb_set(
    "ConfigJson"::jsonb,
    '{syncIntervalMinutes}',
    '1440'::jsonb,
    true
)::text,
"UpdatedAt" = NOW()
WHERE id = 'TU-ID-AQUI'::uuid
  AND "SourceType" = 3;
*/

-- 3. Si necesitas establecer un valor por defecto (15) para todos los que no lo tengan o tengan 60:
/*
UPDATE "InvoiceSourceConfigs"
SET "ConfigJson" = jsonb_set(
    "ConfigJson"::jsonb,
    '{syncIntervalMinutes}',
    '15'::jsonb,
    true
)::text,
"UpdatedAt" = NOW()
WHERE "SourceType" = 3
  AND (
    NOT ("ConfigJson"::jsonb ? 'syncIntervalMinutes')
    OR ("ConfigJson"::jsonb->>'syncIntervalMinutes')::int = 60
  );
*/

