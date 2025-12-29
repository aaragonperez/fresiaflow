# Guía de Configuración de OneDrive para FresiaFlow

## 📋 Introducción

Esta guía te ayudará a configurar la sincronización automática de facturas desde OneDrive hacia FresiaFlow. Una vez configurada, solo necesitas guardar tus facturas en una carpeta de OneDrive y el sistema las procesará automáticamente.

## 🎯 Requisitos Previos

Antes de comenzar, asegúrate de tener:

- ✅ Una cuenta de Microsoft 365 o OneDrive
- ✅ Acceso al [Portal de Azure](https://portal.azure.com)
- ✅ Permisos de administrador en tu tenant de Azure AD (opcional pero recomendado)
- ✅ Una carpeta en OneDrive donde guardarás las facturas

## 🔧 Paso 1: Crear App Registration en Azure

### 1.1 Acceder al Portal de Azure

1. Ve a [portal.azure.com](https://portal.azure.com)
2. Inicia sesión con tu cuenta de Microsoft 365
3. En el menú lateral, busca **Azure Active Directory**

### 1.2 Crear Nueva Aplicación

1. En Azure Active Directory, ve a **App registrations** (Registros de aplicaciones)
2. Haz clic en **+ New registration** (Nuevo registro)
3. Completa el formulario:
   - **Name**: `FresiaFlow OneDrive Sync`
   - **Supported account types**: Selecciona "Accounts in this organizational directory only (Single tenant)"
   - **Redirect URI**: Déjalo en blanco
4. Haz clic en **Register**

### 1.3 Guardar las Credenciales

Una vez creada la aplicación, verás la página de información general:

1. **Copia y guarda** el **Application (client) ID** - Lo necesitarás más tarde
2. **Copia y guarda** el **Directory (tenant) ID** - Lo necesitarás más tarde

## 🔑 Paso 2: Crear Client Secret

### 2.1 Generar el Secret

1. En la página de tu aplicación, ve a **Certificates & secrets** (Certificados y secretos)
2. Haz clic en **+ New client secret** (Nuevo secreto de cliente)
3. Completa:
   - **Description**: `FresiaFlow Sync Secret`
   - **Expires**: Selecciona **24 months** (recomendado)
4. Haz clic en **Add**

### 2.2 Guardar el Secret

⚠️ **IMPORTANTE**: El valor del secret solo se muestra UNA VEZ.

1. **Copia inmediatamente** el **Value** del secret
2. Guárdalo en un lugar seguro (lo necesitarás en FresiaFlow)
3. Si pierdes el secret, tendrás que crear uno nuevo

## 🔐 Paso 3: Configurar Permisos API

### 3.1 Añadir Permisos de Microsoft Graph

1. En la página de tu aplicación, ve a **API permissions** (Permisos de API)
2. Haz clic en **+ Add a permission** (Agregar un permiso)
3. Selecciona **Microsoft Graph**
4. Selecciona **Application permissions** (Permisos de aplicación)
5. Busca y marca los siguientes permisos:
   - ✅ `Files.Read.All` - Para leer archivos de OneDrive

### 3.2 Otorgar Consentimiento de Administrador

⚠️ **Este paso requiere permisos de administrador**

1. Haz clic en **Grant admin consent for [Tu Organización]**
2. Confirma la acción
3. Verifica que el estado muestre "✓ Granted for [Tu Organización]"

Si no tienes permisos de administrador, contacta con tu administrador de IT para que complete este paso.

## ⚙️ Paso 4: Configurar en FresiaFlow

### 4.1 Acceder a la Configuración

1. Abre FresiaFlow en tu navegador
2. Ve a **Configuración** → **Sincronización OneDrive**

### 4.2 Completar los Campos

Introduce los datos que guardaste anteriormente:

| Campo | Valor | Ejemplo |
|-------|-------|---------|
| **Tenant ID** | El Directory (tenant) ID de Azure | `12345678-1234-1234-1234-123456789abc` |
| **Client ID** | El Application (client) ID de Azure | `87654321-4321-4321-4321-cba987654321` |
| **Client Secret** | El secret que copiaste | `abc123...xyz789` |
| **Ruta de Carpeta** | La ruta de tu carpeta en OneDrive | `/Facturas` o `/Documentos/Facturas` |
| **Drive ID** | (Opcional) Solo para SharePoint/Teams | Déjalo vacío para OneDrive personal |

### 4.3 Validar la Conexión

1. Haz clic en **Validar Conexión**
2. Espera unos segundos
3. Si todo está correcto, verás un mensaje como:
   ```
   ✓ Conexión exitosa. Se encontraron 15 archivos de factura en la carpeta.
   ```

### 4.4 Guardar la Configuración

1. Si la validación fue exitosa, haz clic en **Guardar Configuración**
2. Verás un mensaje de confirmación

## 🔄 Paso 5: Configurar la Sincronización

### 5.1 Sincronización Automática

Para que FresiaFlow sincronice automáticamente:

1. Activa el switch **Sincronización Automática Habilitada**
2. Configura el **Intervalo de Sincronización** (mínimo 15 minutos)
   - Recomendado: 30 minutos para uso normal
   - Recomendado: 15 minutos si recibes muchas facturas
3. Haz clic en **Guardar Configuración**

### 5.2 Sincronización Manual

Para sincronizar inmediatamente:

1. Ve a la sección **Sincronización Manual**
2. (Opcional) Marca **Forzar Reprocesamiento** si quieres reprocesar archivos ya sincronizados
3. Haz clic en **Sincronizar Ahora**
4. Observa la barra de progreso en tiempo real

## 📊 Paso 6: Verificar el Historial

### 6.1 Ver Archivos Sincronizados

1. Desplázate a la sección **Historial de Sincronización**
2. Verás una tabla con todos los archivos procesados:
   - **Nombre del archivo**: El nombre original en OneDrive
   - **Estado**: Completado, Procesando, Pendiente, Fallido, Omitido
   - **Tamaño**: Tamaño del archivo
   - **Fecha de sincronización**: Cuándo se procesó

### 6.2 Ver Archivos

Para ver el contenido de un archivo sincronizado:

1. Haz clic en el icono del ojo (👁️) en la columna de acciones
2. El archivo se abrirá en una nueva pestaña

## 💡 Consejos y Buenas Prácticas

### Organización de Archivos

- 📁 Crea una carpeta dedicada solo para facturas (ej: `/Facturas`)
- 📝 Usa nombres descriptivos: `Factura_Amazon_2024-12.pdf`
- 🗂️ Puedes usar subcarpetas; el sistema las explorará recursivamente
- 🗑️ No borres archivos de OneDrive; el sistema los detecta como ya procesados

### Formatos Soportados

El sistema acepta los siguientes formatos:

- ✅ **PDF** (recomendado)
- ✅ **JPG/JPEG**
- ✅ **PNG**
- ✅ **GIF**
- ✅ **WEBP**

### Intervalos de Sincronización

| Volumen de Facturas | Intervalo Recomendado |
|---------------------|----------------------|
| Pocas (< 10/día) | 60 minutos |
| Normal (10-50/día) | 30 minutos |
| Alto (> 50/día) | 15 minutos |

### Detección de Duplicados

El sistema usa un hash del contenido del archivo para detectar duplicados:

- ✅ Si subes el mismo archivo dos veces, solo se procesa una vez
- ✅ Si renombras un archivo, el sistema lo reconoce como el mismo
- ✅ Si modificas el contenido, se trata como un archivo nuevo

## ⚠️ Solución de Problemas

### Error: "No se pudo obtener el token de acceso"

**Causa**: Credenciales incorrectas o expiradas

**Solución**:
1. Verifica que el Tenant ID, Client ID y Client Secret sean correctos
2. Verifica que el Client Secret no haya expirado en Azure
3. Si expiró, crea un nuevo secret y actualiza la configuración

### Error: "Carpeta no encontrada"

**Causa**: La ruta de la carpeta es incorrecta

**Solución**:
1. Verifica que la ruta comience con `/` (ej: `/Facturas`)
2. Verifica que la carpeta exista en OneDrive
3. Respeta mayúsculas/minúsculas en el nombre

### Error: "Permisos insuficientes"

**Causa**: No se otorgó el consentimiento de administrador

**Solución**:
1. Ve a Azure Portal → Tu App → API permissions
2. Haz clic en "Grant admin consent"
3. Si no tienes permisos, contacta con tu administrador

### Los archivos no se procesan

**Causas posibles**:

1. **Formato no soportado**: Verifica que sean PDF o imágenes
2. **Archivo corrupto**: Intenta abrir el archivo manualmente
3. **Sin texto legible**: Si es una imagen, verifica que tenga texto claro
4. **Ya procesado**: El archivo ya fue sincronizado anteriormente

**Solución**:
- Usa la opción "Forzar Reprocesamiento" en la sincronización manual
- Verifica el estado en el historial de sincronización

### Sincronización muy lenta

**Causas posibles**:

1. Muchos archivos grandes
2. Conexión a Internet lenta
3. Servidor de OneDrive con alta latencia

**Solución**:
- Reduce el tamaño de los archivos PDF (usa compresión)
- Aumenta el intervalo de sincronización
- Sincroniza en horarios de menor uso

## 🔒 Seguridad y Privacidad

### ¿Qué datos accede FresiaFlow?

- ✅ **Solo lectura** de la carpeta configurada
- ✅ **No puede modificar** ni eliminar archivos
- ✅ **No accede** a otras carpetas de OneDrive
- ✅ **No accede** a tu correo, calendario u otros servicios

### ¿Dónde se almacenan las credenciales?

- Las credenciales se almacenan **encriptadas** en la base de datos de FresiaFlow
- Solo se usan para conectar con Microsoft Graph API
- Nunca se comparten con terceros

### ¿Puedo revocar el acceso?

Sí, en cualquier momento:

1. Ve a Azure Portal → Tu App → Overview
2. Haz clic en "Delete"
3. O simplemente desactiva la sincronización en FresiaFlow

## 📞 Soporte

Si tienes problemas o dudas:

1. Consulta la sección **FAQ** en la ayuda de FresiaFlow
2. Revisa los logs en el historial de sincronización
3. Contacta con el soporte técnico de Fresia Software Solutions

---

**Última actualización**: Diciembre 2025  
**Versión**: 1.2.0

