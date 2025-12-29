-- Script para verificar y corregir syncIntervalMinutes en OneDrive
-- Ejecutar en la base de datos de FresiaFlow

-- 1. Verificar el valor actual en InvoiceSourceConfigs
SELECT 
    "Id",
    "Name",
    "SourceType",
    "ConfigJson",
    "Enabled",
    "LastSyncAt"
FROM "InvoiceSourceConfigs"
WHERE "SourceType" = 3; -- InvoiceSourceType.OneDrive = 3

-- 2. Ver el valor específico de syncIntervalMinutes en el JSON
SELECT 
    "Id",
    "Name",
    "ConfigJson"::jsonb->>'syncIntervalMinutes' as "SyncIntervalMinutes",
    "ConfigJson"
FROM "InvoiceSourceConfigs"
WHERE "SourceType" = 3;

-- 3. Actualizar syncIntervalMinutes a 15 (si está en 60)
UPDATE "InvoiceSourceConfigs"
SET "ConfigJson" = jsonb_set(
    "ConfigJson"::jsonb,
    '{syncIntervalMinutes}',
    '15',
    true
)::text,
    "UpdatedAt" = NOW()
WHERE "SourceType" = 3
  AND ("ConfigJson"::jsonb->>'syncIntervalMinutes')::int = 60;

-- 4. Verificar después de la actualización
SELECT 
    "Id",
    "Name",
    "ConfigJson"::jsonb->>'syncIntervalMinutes' as "SyncIntervalMinutes"
FROM "InvoiceSourceConfigs"
WHERE "SourceType" = 3;

