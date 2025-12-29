# Guía de Usuario de FresiaFlow

## 🏠 Introducción

**FresiaFlow** es tu secretaria administrativa virtual diseñada específicamente para micro-pymes. Automatiza la gestión de facturas, conciliación bancaria y tareas administrativas mediante inteligencia artificial.

### Características Principales

- 📄 **Extracción automática de datos** de facturas PDF e imágenes
- 📊 **Estadísticas en tiempo real** de facturación, IVA y totales
- ☁️ **Sincronización con OneDrive** para carga automática de facturas
- 💰 **Conexión bancaria** mediante Open Banking (PSD2)
- ✅ **Gestión de tareas** con prioridades y recordatorios
- 💬 **Chat con IA** para consultas en lenguaje natural
- 🎨 **Temas personalizables** (Claro, Oscuro, Azul, Verde, Púrpura)

---

## 📊 Dashboard

El Dashboard es tu panel de control principal. Aquí encontrarás:

### Tarjetas de Resumen

- **Tareas Pendientes**: Número de tareas que requieren atención
- **Alta Prioridad**: Contador de elementos urgentes (en rojo)
- **Alertas Críticas**: Avisos importantes del sistema
- **Saldo Total**: Resumen de cuentas bancarias conectadas

### Secciones Principales

1. **Lista de Tareas**: Facturas que necesitan revisión manual
2. **Resumen Bancario**: Estado de bancos conectados
3. **Alertas del Sistema**: Notificaciones importantes
4. **Estado de Sincronización**: Información sobre OneDrive

### Consejos de Uso

- ✅ Revisa el Dashboard diariamente
- ✅ Atiende primero las alertas críticas
- ✅ Las tareas de alta prioridad aparecen destacadas

---

## 📄 Gestión de Facturas

### Panel de Estadísticas

En la parte superior verás 7 tarjetas informativas:

| Tarjeta | Descripción |
|---------|-------------|
| **Total Facturas** | Número total de facturas en el sistema |
| **Pago Banco** | Facturas pagadas mediante transferencia |
| **Pago Efectivo** | Facturas pagadas en efectivo |
| **Baja Confianza** | Facturas que necesitan revisión manual |
| **Total Facturado** | Suma de todos los importes |
| **Total IVA** | IVA acumulado de todas las facturas |
| **Base Imponible** | Suma de todas las bases imponibles |

### Filtros Contables

Filtra tus facturas por:

- **Año Fiscal**: Selecciona el año a consultar
- **Trimestre**: Q1 (Ene-Mar), Q2 (Abr-Jun), Q3 (Jul-Sep), Q4 (Oct-Dic)
- **Proveedor**: Busca por nombre de proveedor
- **Tipo de Pago**: Banco o Efectivo

### Subir Facturas Manualmente

#### Métodos de Carga

1. **Arrastrar y Soltar**: Arrastra archivos al área de carga
2. **Clic para Seleccionar**: Haz clic y selecciona archivos

#### Formatos Soportados

- ✅ PDF (recomendado)
- ✅ JPG/JPEG
- ✅ PNG
- ✅ GIF
- ✅ WEBP

#### Proceso de Carga

1. Selecciona uno o varios archivos
2. El sistema los sube automáticamente
3. La IA extrae los datos de cada factura
4. Revisa los datos extraídos
5. Edita manualmente si es necesario
6. Guarda la factura

### Tabla de Facturas

#### Funcionalidades

- **Ordenación**: Haz clic en las cabeceras de columna
- **Filtrado Global**: Busca en todas las columnas a la vez
- **Filtrado por Columna**: Filtra cada columna individualmente
- **Paginación**: Navega entre páginas (10, 25 o 50 facturas por página)
- **Detalle de Líneas**: Expande para ver las líneas de cada factura
- **Acciones**: Editar o eliminar facturas

#### Columnas Principales

- Número de Factura
- Proveedor
- Fecha de Emisión
- Fecha de Vencimiento
- Importe Total
- IVA
- Base Imponible
- Tipo de Pago
- Confianza (%)

### Editar Facturas

Para editar una factura:

1. Haz clic en el icono de lápiz (✏️)
2. Modifica los campos necesarios
3. Edita las líneas de la factura si es necesario
4. Haz clic en **Guardar**

### Exportar a Excel

1. Haz clic en **Exportar Excel**
2. El archivo se descargará automáticamente
3. Incluye todas las facturas filtradas actualmente

---

## ☁️ Sincronización con OneDrive

### Configuración Inicial

Para configurar la sincronización con OneDrive, consulta la [Guía de Configuración de OneDrive](./onedrive-setup-guide.md).

### Sincronización Automática

Una vez configurada:

1. Activa el switch **Sincronización Automática**
2. Configura el **Intervalo de Sincronización** (mínimo 15 minutos)
3. El sistema sincronizará automáticamente en el intervalo configurado

### Sincronización Manual

Para sincronizar inmediatamente:

1. Ve a **Configuración** → **Sincronización OneDrive**
2. Haz clic en **Sincronizar Ahora**
3. (Opcional) Marca **Forzar Reprocesamiento** para reprocesar archivos ya sincronizados
4. Observa el progreso en tiempo real

### Historial de Sincronización

La tabla de historial muestra:

- **Nombre del archivo**: Nombre original en OneDrive
- **Estado**: Completado, Procesando, Pendiente, Fallido, Omitido
- **Tamaño**: Tamaño del archivo
- **Fecha de sincronización**: Cuándo se procesó
- **Acciones**: Ver el archivo original

### Estados de Archivos

| Estado | Descripción |
|--------|-------------|
| **Completado** | Archivo procesado exitosamente |
| **Procesando** | Archivo en proceso de extracción |
| **Pendiente** | Archivo en cola de procesamiento |
| **Fallido** | Error al procesar el archivo |
| **Omitido** | Archivo ya procesado anteriormente |

---

## ✅ Gestión de Tareas

### Tipos de Tareas

1. **Revisión de Facturas**: Facturas con baja confianza (< 80%)
2. **Conciliación Bancaria**: Transacciones sin asociar
3. **Tareas Administrativas**: Recordatorios personalizados
4. **Alertas del Sistema**: Notificaciones que requieren acción

### Prioridades

- 🔴 **Alta**: Requieren atención inmediata
- 🟠 **Media**: Importantes pero no urgentes
- 🔵 **Baja**: Pueden esperar

### Gestión

- ✅ Marca como completadas con el checkbox
- ✏️ Edita para cambiar prioridad o descripción
- 🗑️ Elimina tareas no relevantes
- 🔍 Filtra por prioridad o estado

### Notificaciones Automáticas

El sistema genera tareas cuando:

- Una factura tiene confianza < 80%
- Hay transacciones bancarias sin conciliar
- Se detectan anomalías o errores
- Hay recordatorios programados

---

## 💰 Conexión Bancaria

### Seguridad

La conexión utiliza **Open Banking (PSD2)**, el estándar europeo:

- ✅ No almacenamos credenciales bancarias
- ✅ Conexión directa y segura con tu banco
- ✅ Solo acceso de lectura
- ✅ Autorización explícita requerida
- ✅ Puedes revocar el acceso en cualquier momento

### Funcionalidades

- **Importación de Movimientos**: Descarga automática de transacciones
- **Conciliación Automática**: Asocia pagos con facturas
- **Múltiples Cuentas**: Conecta todas tus cuentas
- **Saldo en Tiempo Real**: Visualiza el saldo actualizado
- **Historial Completo**: Accede a todos tus movimientos

### Conciliación

El sistema intenta automáticamente asociar transacciones con facturas:

- ✅ Coincidencia por importe exacto
- ✅ Coincidencia por fecha cercana
- ✅ Coincidencia por concepto/proveedor
- ✅ Sugerencias inteligentes con IA

---

## 💬 Chat con IA (Fresia)

### ¿Qué puede hacer Fresia?

- 📊 Consultas sobre facturas y estadísticas
- ❓ Ayuda con el uso de la aplicación
- 📈 Análisis de datos bajo demanda
- 🛠️ Soporte técnico y dudas frecuentes

### Ejemplos de Preguntas

```
"¿Cuál es el proveedor con mayor facturación?"
"¿Cuánto IVA he pagado este trimestre?"
"¿Hay facturas pendientes de revisión?"
"Muéstrame las facturas de Amazon"
"¿Cuánto he gastado en el segundo trimestre?"
```

### Consejos de Uso

- 💡 Sé específico en tus preguntas
- 💡 El chat recuerda el contexto de la conversación
- 💡 Puedes minimizar el chat y retomar después
- 💡 Está disponible en todas las pantallas

---

## ⚙️ Configuración

### Empresas Propias

Configura los nombres de tus empresas para que el sistema las reconozca:

1. Ve a **Configuración** → **Empresas Propias**
2. Añade todas las variantes del nombre de tu empresa
3. Las facturas con estos nombres como proveedor serán ignoradas

**Ejemplo**:
- FRESIA SOFTWARE SOLUTIONS
- Fresia Software Solutions
- Fresia Software

### Sincronización OneDrive

Consulta la [Guía de Configuración de OneDrive](./onedrive-setup-guide.md) para instrucciones detalladas.

### Selector de Tema

Personaliza la apariencia de la aplicación:

1. En la barra lateral, busca el **Selector de Tema**
2. Elige entre:
   - 🌞 **Claro**: Tema por defecto con colores claros
   - 🌙 **Oscuro**: Tema oscuro para reducir fatiga visual
   - 🔵 **Azul**: Tema profesional con tonos azules
   - 🟢 **Verde**: Tema fresco con tonos verdes
   - 🟣 **Púrpura**: Tema elegante con tonos púrpura

El tema se aplica instantáneamente a toda la aplicación.

---

## ❓ Preguntas Frecuentes (FAQ)

### ¿Qué formatos de factura acepta el sistema?

FresiaFlow acepta **PDF** e imágenes (**JPG, PNG, GIF, WEBP**). El sistema extrae automáticamente el texto mediante OCR y la IA estructura los datos.

### ¿Cómo funciona la extracción con IA?

Usamos **OpenAI GPT-4o-mini** para analizar el contenido de las facturas y extraer datos estructurados: número de factura, fechas, importes, IVA, proveedor, NIF/CIF, etc. El sistema calcula un nivel de confianza y marca para revisión las facturas con confianza menor al 80%.

### ¿Qué significa "Baja Confianza" en una factura?

Indica que la IA no pudo extraer todos los datos con certeza. Esto puede ocurrir por:

- Calidad baja del PDF o imagen
- Formato de factura no estándar
- Datos ilegibles o borrosos
- Idiomas no soportados

**Solución**: Revisa y corrige manualmente estos campos antes de guardar.

### ¿Es seguro conectar mi banco?

Sí. Usamos estándares **Open Banking (PSD2)** que requieren tu autorización explícita. Las credenciales bancarias nunca se almacenan en nuestro sistema. Solo accedemos a los movimientos que autorices mediante APIs seguras del banco.

### ¿Puedo exportar mis datos?

Sí, puedes exportar facturas a **Excel** usando el botón "Exportar Excel" en la pantalla de Facturas. El archivo incluye todos los datos estructurados para importar en tu gestoría contable.

### ¿Cómo cambio el tema de la aplicación?

En la barra lateral (sidebar), encontrarás el selector "Tema" con un desplegable. Selecciona el tema que prefieras y se aplicará instantáneamente.

### ¿Qué hago si la extracción es incorrecta?

Puedes editar manualmente cualquier campo usando el botón de editar (✏️) en la tabla de facturas. El sistema mejora con el tiempo basándose en las correcciones.

### ¿Cómo funciona la sincronización con OneDrive?

La sincronización conecta tu cuenta de Microsoft 365 con FresiaFlow. Una vez configurada, el sistema revisa automáticamente una carpeta específica de OneDrive en intervalos regulares (mínimo 15 minutos) y procesa todas las facturas nuevas que encuentre.

Los archivos ya procesados se detectan automáticamente mediante hash, evitando duplicados.

### ¿Es seguro conectar mi OneDrive?

Sí. La conexión se realiza mediante **Azure Active Directory** con permisos específicos de solo lectura. Las credenciales se almacenan de forma segura y solo se usan para acceder a la carpeta específica que configures.

### ¿Qué pasa si un archivo falla al procesarse desde OneDrive?

Si un archivo falla, se marca como "Fallido" en el historial de sincronización. Puedes intentar reprocesar el archivo usando la opción "Forzar Reprocesamiento". Si el problema persiste, verifica que el archivo sea un formato válido y que contenga texto legible.

### ¿Puedo usar OneDrive de empresa o SharePoint?

Sí. FresiaFlow soporta OneDrive personal, OneDrive for Business y SharePoint. Para SharePoint o Teams, necesitarás proporcionar el **Drive ID** además de la ruta de la carpeta.

---

## 🔧 Información Técnica

### Especificaciones

| Componente | Tecnología |
|------------|------------|
| **Versión** | 1.2.0 |
| **Arquitectura** | Hexagonal (Ports & Adapters) |
| **Backend** | ASP.NET Core 8.0 (C#) |
| **Frontend** | Angular 17 + PrimeNG |
| **Base de datos** | PostgreSQL |
| **IA** | OpenAI GPT-4o-mini |
| **Bancos** | Open Banking AIS (PSD2) |
| **Sincronización** | Microsoft Graph API + SignalR |

### Novedades v1.2.0

- ☁️ **Sincronización con OneDrive**: Automatiza la carga de facturas
- 📡 **Progreso en tiempo real**: Visualiza el progreso con SignalR
- 📊 **Historial de sincronización**: Tabla completa de archivos procesados
- 🔄 **Sincronización automática**: Intervalos personalizados
- 🎯 **Detección de duplicados**: Sistema de hash de archivos
- 🚀 **Validación de conexión**: Prueba antes de guardar
- ⚙️ **Sistema de agentes IA**: Router inteligente para desarrollo

---

## 📞 Soporte

Si necesitas ayuda adicional:

1. Consulta la ayuda integrada en la aplicación (botón "Ayuda" en la barra lateral)
2. Revisa esta documentación
3. Contacta con el soporte técnico de Fresia Software Solutions

---

**Desarrollado con ❤️ por Fresia Software Solutions**  
**Última actualización**: Diciembre 2025  
**Versión**: 1.2.0

