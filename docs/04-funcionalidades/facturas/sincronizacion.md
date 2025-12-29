# ¿Cómo Funciona la Sincronización de Facturas?

## 📍 ¿Dónde se Guardan los Archivos?

Los archivos descargados **NO se guardan en una carpeta visible directamente**. El proceso es el siguiente:

1. **Descarga temporal en memoria**: Los archivos se descargan desde el portal (Orange, etc.) directamente a memoria
2. **Procesamiento inmediato**: Se procesan con IA para extraer datos
3. **Almacenamiento final**: Se guardan en la carpeta `./uploads` (configurada en `appsettings.json`) con un nombre único: `{GUID}_{nombre_original}.pdf`
4. **Base de datos**: Se registra en la tabla `SyncedFiles` para evitar duplicados

**Ubicación de archivos:**
- **Carpeta de uploads**: `src/FresiaFlow.Api/uploads/` (relativo al directorio de ejecución)
- **Configuración**: `appsettings.json` → `Storage:BasePath: "./uploads"`

## 🔄 ¿Qué Hace Realmente la Sincronización?

### Para Portales Web (como Orange):

1. **Abre el navegador** (Playwright con Chromium en modo headless)
2. **Navega al portal** (ej: `https://areaclientes.orange.es/login`)
3. **Inicia sesión** automáticamente (si hay credenciales configuradas)
4. **Busca enlaces a facturas** usando selectores CSS configurados
5. **Descarga cada factura** encontrada
6. **Procesa cada factura**:
   - Guarda el archivo PDF en `./uploads/{GUID}_{nombre}.pdf`
   - Extrae datos con IA (OpenAI): número, proveedor, fechas, importes, IVA, IRPF, líneas de detalle
   - Crea un registro en la base de datos (`InvoiceReceived`)
   - Registra en `SyncedFiles` para evitar descargar de nuevo
7. **Actualiza estadísticas**: Total procesadas, fallidas, omitidas

### Flujo Completo:

```
Portal Web (Orange)
    ↓
Playwright (navegador automático)
    ↓
Descarga PDF → Memoria
    ↓
UploadInvoiceUseCase
    ↓
1. Guarda archivo → ./uploads/{GUID}_factura.pdf
    ↓
2. Extrae datos con IA (OpenAI)
    ↓
3. Crea InvoiceReceived en BD
    ↓
4. Registra en SyncedFiles
```

## 📊 ¿Cómo Saber que Está Sincronizando?

### En el Frontend (Interfaz Web):

La sincronización muestra progreso en tiempo real mediante **SignalR**:

1. **Barra de progreso**: Muestra porcentaje (0-100%)
2. **Contador**: "X / Y archivos" procesados
3. **Archivo actual**: Nombre del archivo que se está procesando
4. **Estado**: 
   - 🔄 "Sincronizando..." (spinner animado)
   - ✅ "Completado"
   - ❌ "Error"
   - ⏸️ "Pausado"

**Ubicación en el código:**
- Frontend: `apps/fresiaflow-web/ui/pages/import-page/import-page.component.html` (líneas 108-145)
- Backend: `src/FresiaFlow.Adapters/Inbound/Api/Notifiers/SignalRSyncProgressNotifier.cs`

### En los Logs del Backend:

Puedes ver los logs en la consola donde ejecutas la API:

```
[INFO] Iniciando sincronización de portal: ORANGE
[INFO] Se encontraron 5 facturas en el portal
[INFO] Procesando: factura_001.pdf
[INFO] Procesando: factura_002.pdf
...
```

### En la Base de Datos:

Puedes consultar la tabla `SyncedFiles` para ver qué archivos se han sincronizado:

```sql
SELECT 
    Source,
    FileName,
    Status,
    SyncedAt,
    InvoiceId
FROM "SyncedFiles"
WHERE Source LIKE 'Portal-%'
ORDER BY SyncedAt DESC;
```

## 🗂️ Estructura de Datos

### Tabla `SyncedFiles`:
- **Source**: `"Portal-{sourceId}"` (ej: "Portal-123e4567-e89b-12d3-a456-426614174000")
- **ExternalId**: URL del archivo en el portal
- **FileName**: Nombre del archivo descargado
- **Status**: `Pending`, `Processing`, `Completed`, `Failed`
- **InvoiceId**: ID de la factura creada (si se procesó correctamente)

### Tabla `InvoiceReceived`:
- Contiene todas las facturas procesadas
- Incluye datos extraídos: proveedor, importes, IVA, IRPF, líneas de detalle
- Campo `Origin`: Indica si viene de `ManualUpload`, `OneDrive`, `Portal`, etc.

## 🔍 Verificar el Estado de la Sincronización

### 1. En la Interfaz Web:
- Ve a "Fuentes de Facturas"
- Mira la columna "Última Sincronización" en la tabla
- Mira "Archivos Procesados" para ver cuántos se han descargado

### 2. En la Base de Datos:
```sql
-- Ver todas las fuentes y su estado
SELECT 
    Name,
    SourceType,
    Enabled,
    LastSyncAt,
    LastSyncError,
    TotalFilesSynced
FROM "InvoiceSourceConfigs";

-- Ver archivos sincronizados de un portal específico
SELECT 
    sf.FileName,
    sf.Status,
    sf.SyncedAt,
    ir."InvoiceNumber",
    ir."SupplierName"
FROM "SyncedFiles" sf
LEFT JOIN "InvoiceReceived" ir ON sf."InvoiceId" = ir."Id"
WHERE sf.Source = 'Portal-{TU_SOURCE_ID}'
ORDER BY sf."SyncedAt" DESC;
```

### 3. En los Archivos:
Revisa la carpeta `src/FresiaFlow.Api/uploads/` para ver los PDFs descargados.

## ⚠️ Problemas Comunes

### "No se ve progreso en la interfaz"
- Verifica que SignalR esté conectado (consola del navegador: F12 → Console)
- Revisa los logs del backend para ver si hay errores

### "Los archivos no aparecen en la lista de facturas"
- Verifica que la extracción con IA haya funcionado (revisa logs)
- Revisa la tabla `InvoiceReceived` en la BD
- Verifica que no haya errores en `SyncedFiles.Status = 'Failed'`

### "La sincronización se queda colgada"
- Revisa los timeouts de Playwright (ahora configurados a 60 segundos)
- Verifica que el portal esté accesible
- Revisa los logs para ver en qué paso se quedó

## 📝 Resumen

**¿Dónde se guardan?** → `./uploads/` (carpeta relativa al ejecutable de la API)

**¿Qué hace?** → Descarga PDFs del portal, los procesa con IA, y crea facturas en la BD

**¿Cómo ver el progreso?** → Barra de progreso en tiempo real en la interfaz web (SignalR)

**¿Dónde ver los resultados?** → Tabla de facturas en la interfaz web o consultando `InvoiceReceived` en la BD

