# 🔄 Propuesta: Unificación de Fuentes de Importación

## 📋 Situación Actual

Actualmente existen **dos sistemas separados** para importar facturas:

### 1. **Sistema de Fuentes** (`InvoiceSources`)
- **Tipos**: Email, Portal, WebScraping
- **Configuración**: Tabla `InvoiceSourceConfigs` (múltiples fuentes)
- **Controlador**: `/api/invoice-sources`
- **UI**: Componente `invoice-sources-config`
- **Sincronización**: `SyncInvoicesFromSourcesUseCase` (sincroniza todas las fuentes habilitadas)

### 2. **Sistema OneDrive** (Separado)
- **Configuración**: Tabla `OneDriveSyncConfigs` (una sola configuración)
- **Controlador**: `/api/sync/onedrive`
- **UI**: Componente `onedrive-config` (en settings)
- **Sincronización**: `OneDriveSyncBackgroundService` (solo OneDrive)

## ❌ Problemas Actuales

1. **Duplicación de código**: Ambos sistemas hacen básicamente lo mismo (descargar archivos y procesarlos)
2. **Configuración fragmentada**: El usuario debe ir a dos lugares diferentes para configurar fuentes
3. **Inconsistencia**: OneDrive no aparece en la lista de fuentes, aunque es una fuente más
4. **Mantenimiento complejo**: Dos sistemas paralelos que requieren mantenimiento separado
5. **Falta de coherencia**: No hay una visión unificada de todas las fuentes de importación

## ✅ Solución Propuesta

### Unificar OneDrive como un tipo más de fuente

**OneDrive debe ser tratado como `InvoiceSourceType.OneDrive`**, igual que Email, Portal y WebScraping.

## 🏗️ Arquitectura Propuesta

### 1. **Dominio** (`FresiaFlow.Domain`)

#### Actualizar `InvoiceSourceType`:
```csharp
public enum InvoiceSourceType
{
    Email,
    Portal,
    WebScraping,
    OneDrive  // ← NUEVO
}
```

#### Migrar `OneDriveSyncConfig` → `InvoiceSourceConfig`:
- La configuración de OneDrive se guardará como JSON en `InvoiceSourceConfig.ConfigJson`
- Formato JSON propuesto:
```json
{
  "tenantId": "...",
  "clientId": "...",
  "clientSecret": "...",
  "folderPath": "/carpeta/facturas",
  "driveId": null,
  "syncIntervalMinutes": 15
}
```

### 2. **Adaptadores** (`FresiaFlow.Adapters`)

#### Crear `OneDriveSyncService` que implemente `IInvoiceSourceSyncService`:
- Adaptar el `OneDriveSyncService` actual para que implemente la interfaz común
- Métodos requeridos:
  - `GetConfigAsync(Guid sourceId)` - Lee desde `InvoiceSourceConfig`
  - `SaveConfigAsync(InvoiceSourceConfig config)` - Guarda en `InvoiceSourceConfig`
  - `GetSyncPreviewAsync(Guid sourceId)` - Preview de archivos
  - `SyncNowAsync(Guid sourceId, bool forceReprocess)` - Sincronización
  - `ValidateConfigAsync(string configJson)` - Validación

#### Actualizar `InvoiceSourceSyncServiceFactory`:
```csharp
public IInvoiceSourceSyncService GetService(InvoiceSourceType sourceType)
{
    return sourceType switch
    {
        InvoiceSourceType.Email => _emailSyncService,
        InvoiceSourceType.WebScraping => _webScrapingSyncService,
        InvoiceSourceType.Portal => _portalSyncService,
        InvoiceSourceType.OneDrive => _oneDriveSyncService,  // ← NUEVO
        _ => throw new ArgumentException($"Tipo de fuente desconocido: {sourceType}")
    };
}
```

#### Actualizar `SyncInvoicesFromSourcesUseCase`:
- Eliminar el switch manual y usar el factory
- OneDrive se sincronizará automáticamente junto con las demás fuentes

### 3. **API** (`FresiaFlow.Api`)

#### Opción A: Mantener controlador separado (temporal)
- Mantener `OneDriveSyncController` para compatibilidad durante la migración
- Internamente, redirigir a `InvoiceSourcesController`

#### Opción B: Eliminar controlador separado (recomendado)
- Eliminar `OneDriveSyncController`
- Usar solo `InvoiceSourcesController` para todas las fuentes
- Endpoints unificados:
  - `GET /api/invoice-sources` - Lista todas (incluye OneDrive)
  - `POST /api/invoice-sources` - Crear/actualizar (incluye OneDrive)
  - `POST /api/invoice-sources/{id}/sync` - Sincronizar cualquier fuente

### 4. **Frontend** (`fresiaflow-web`)

#### Unificar UI en un solo componente:
- **Eliminar**: `onedrive-config.component`
- **Actualizar**: `invoice-sources-config.component` para incluir OneDrive
- Agregar OneDrive a la lista de tipos de fuente:
```typescript
sourceTypes = [
  { label: 'Email (IMAP/POP3)', value: 'Email' },
  { label: 'Portal Web', value: 'Portal' },
  { label: 'Web Scraping', value: 'WebScraping' },
  { label: 'OneDrive / SharePoint', value: 'OneDrive' }  // ← NUEVO
];
```

#### Formulario específico para OneDrive:
- Cuando el tipo es `OneDrive`, mostrar campos específicos:
  - Tenant ID
  - Client ID
  - Client Secret
  - Folder Path
  - Drive ID (opcional)
  - Sync Interval (minutos)

### 5. **Background Services**

#### Actualizar `OneDriveSyncBackgroundService`:
- En lugar de leer `OneDriveSyncConfig`, leer `InvoiceSourceConfig` con tipo `OneDrive`
- Solo procesar fuentes habilitadas
- O mejor: **eliminar** este servicio y usar un servicio unificado que procese todas las fuentes

#### Crear `InvoiceSourcesSyncBackgroundService` (opcional):
- Servicio unificado que sincroniza todas las fuentes habilitadas periódicamente
- Reemplaza tanto `OneDriveSyncBackgroundService` como cualquier lógica de sincronización automática

## 📊 Migración de Datos

### Script de migración SQL:

```sql
-- 1. Migrar configuración de OneDrive a InvoiceSourceConfig
INSERT INTO InvoiceSourceConfigs (Id, SourceType, Name, ConfigJson, Enabled, LastSyncAt, LastSyncError, TotalFilesSynced, CreatedAt, UpdatedAt)
SELECT 
    Id,
    'OneDrive' as SourceType,
    'OneDrive Sync' as Name,
    json_object(
        'tenantId', TenantId,
        'clientId', ClientId,
        'clientSecret', ClientSecret,
        'folderPath', FolderPath,
        'driveId', DriveId,
        'syncIntervalMinutes', SyncIntervalMinutes
    ) as ConfigJson,
    Enabled,
    LastSyncAt,
    LastSyncError,
    TotalFilesSynced,
    CreatedAt,
    UpdatedAt
FROM OneDriveSyncConfigs
WHERE EXISTS (SELECT 1 FROM OneDriveSyncConfigs LIMIT 1);

-- 2. Migrar archivos sincronizados (opcional, si hay tabla SyncedFiles específica)
-- Actualizar Source en SyncedFiles de "OneDrive" a "OneDrive-{sourceId}"
```

## 🎯 Beneficios

1. ✅ **Un solo lugar para configurar todas las fuentes**
2. ✅ **Código más mantenible** (un solo sistema)
3. ✅ **Consistencia** (todas las fuentes funcionan igual)
4. ✅ **Extensibilidad** (fácil agregar nuevas fuentes)
5. ✅ **Mejor UX** (interfaz unificada)
6. ✅ **Menos duplicación** (código DRY)

## 📝 Plan de Implementación

### Fase 1: Preparación
1. Crear migración de base de datos
2. Actualizar enum `InvoiceSourceType`
3. Crear adaptador `OneDriveSyncService` que implemente `IInvoiceSourceSyncService`

### Fase 2: Backend
1. Actualizar `InvoiceSourceSyncServiceFactory`
2. Actualizar `SyncInvoicesFromSourcesUseCase`
3. Migrar datos de `OneDriveSyncConfig` a `InvoiceSourceConfig`
4. Actualizar `OneDriveSyncBackgroundService` o crear servicio unificado

### Fase 3: API
1. Actualizar `InvoiceSourcesController` para soportar OneDrive
2. Marcar `OneDriveSyncController` como obsoleto (o eliminar)

### Fase 4: Frontend
1. Actualizar `invoice-sources-config.component` para incluir OneDrive
2. Agregar formulario específico para configuración de OneDrive
3. Eliminar `onedrive-config.component`
4. Actualizar routing y navegación

### Fase 5: Limpieza
1. Eliminar `OneDriveSyncConfig` (después de migración)
2. Eliminar `OneDriveSyncController` (si se mantuvo)
3. Actualizar documentación

## ⚠️ Consideraciones

1. **Compatibilidad hacia atrás**: Durante la migración, mantener ambos sistemas funcionando
2. **Validación**: Asegurar que la configuración JSON de OneDrive sea válida
3. **Testing**: Probar exhaustivamente la migración de datos
4. **Documentación**: Actualizar docs de configuración

## 🔗 Archivos a Modificar

### Backend:
- `src/FresiaFlow.Domain/Sync/InvoiceSourceType.cs`
- `src/FresiaFlow.Domain/Sync/InvoiceSourceConfig.cs`
- `src/FresiaFlow.Adapters/Outbound/OneDrive/OneDriveSyncService.cs` (refactorizar)
- `src/FresiaFlow.Adapters/Outbound/InvoiceSources/InvoiceSourceSyncServiceFactory.cs`
- `src/FresiaFlow.Adapters/Outbound/InvoiceSources/SyncInvoicesFromSourcesUseCase.cs`
- `src/FresiaFlow.Adapters/Inbound/Api/Controllers/InvoiceSourcesController.cs`
- `src/FresiaFlow.Adapters/Inbound/Api/Controllers/OneDriveSyncController.cs` (eliminar o marcar obsoleto)
- `src/FresiaFlow.Adapters/Outbound/OneDrive/OneDriveSyncBackgroundService.cs` (refactorizar)

### Frontend:
- `apps/fresiaflow-web/ui/components/invoice-sources-config/invoice-sources-config.component.ts`
- `apps/fresiaflow-web/ui/components/invoice-sources-config/invoice-sources-config.component.html`
- `apps/fresiaflow-web/infrastructure/services/invoice-sources.service.ts`
- `apps/fresiaflow-web/ui/components/onedrive-config/` (eliminar)
- `apps/fresiaflow-web/ui/pages/settings-page/` (eliminar referencia a OneDrive)

### Base de Datos:
- Migración: `src/FresiaFlow.Adapters/Outbound/Persistence/Migrations/` (nueva migración)

