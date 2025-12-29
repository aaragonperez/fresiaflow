# 📱 Funcionalidades de WhatsApp - FresiaFlow

## 🎯 Resumen

FresiaFlow ahora puede enviarte notificaciones por WhatsApp cuando haya **tareas pendientes de validar**.

---

## ✨ Características Implementadas

### 1. 🔔 Notificación al Crear Tarea
Cada vez que se crea una tarea nueva, recibes un WhatsApp instantáneo con:
- 📋 Título de la tarea
- 📝 Descripción
- ⚠️ Nivel de prioridad (Urgente/Alta/Media/Baja)
- 📅 Fecha de vencimiento (si existe)

**Ejemplo de mensaje:**
```
🔴 Nueva Tarea Pendiente

📋 Verificar factura FAC-2024-001

Factura de proveedor ABC con confianza 
de extracción baja (65%)

⏰ Prioridad: Urgente
📅 Vencimiento: 25/12/2024
```

### 2. 📊 Resumen de Tareas Pendientes
Envía un resumen consolidado de todas las tareas pendientes con:
- Total de tareas
- Desglose por prioridad (🔴 Urgente, 🟠 Alta, 🟡 Media, 🟢 Baja)
- Top 3 tareas más prioritarias

**Ejemplo de mensaje:**
```
📊 Resumen de Tareas Pendientes

Total: 12 tareas

🔴 Urgente: 3
🟠 Alta: 5
🟡 Media: 3
🟢 Baja: 1

Tareas prioritarias:
1. 🔴 Verificar factura FAC-001
2. 🔴 Revisar proveedor desconocido
3. 🟠 Reconciliar transacción
```

### 3. ✅ Mensaje de Prueba
Prueba tu configuración enviando un mensaje de verificación.

### 4. 🔍 Verificación de Estado
Endpoint para verificar si WhatsApp está correctamente configurado.

---

## 🚀 Casos de Uso Automáticos

### Facturas con Baja Confianza
Cuando se procesa una factura y la IA tiene confianza < 70%:
1. Se crea automáticamente una tarea
2. Se envía notificación por WhatsApp
3. Puedes revisar y corregir desde la app

### Proveedores Desconocidos
Cuando una factura tiene proveedor desconocido:
1. Se genera tarea de verificación
2. Recibes WhatsApp con detalles
3. Puedes identificar el proveedor correctamente

### Sincronización OneDrive
Si hay errores al sincronizar archivos desde OneDrive:
1. Se crea tarea de revisión
2. Notificación instantánea
3. Puedes corregir el problema rápidamente

---

## 🎛️ Configuración

### Parámetros Disponibles

```json
{
  "WhatsApp": {
    "Enabled": true,                    // Activar/desactivar
    "PhoneNumberId": "123...",          // Phone Number ID de Meta
    "AccessToken": "EAAx...",           // Access Token de Meta
    "RecipientPhone": "56912345678",    // Tu número
    "SendOnTaskCreation": true,         // Notificar al crear tarea
    "SendDailySummary": false,          // Resumen diario (próximamente)
    "DailySummaryTime": "09:00"         // Hora del resumen (próximamente)
  }
}
```

### Activar/Desactivar Rápidamente
```json
{
  "WhatsApp": {
    "Enabled": false  // Simplemente cambia a false para desactivar
  }
}
```

---

## 📡 API Endpoints

### `GET /api/whatsapp/status`
Verifica el estado de la configuración.

**Respuesta:**
```json
{
  "isConfigured": true,
  "isEnabled": true,
  "phoneNumberId": "Configurado",
  "recipientPhone": "56912345678"
}
```

### `POST /api/whatsapp/test`
Envía un mensaje de prueba.

**Request:**
```json
{
  "recipientPhone": "56912345678"  // Opcional, usa el configurado si no se envía
}
```

**Respuesta:**
```json
{
  "message": "Mensaje de prueba enviado exitosamente"
}
```

### `POST /api/whatsapp/send-tasks-summary`
Envía resumen de tareas pendientes.

**Respuesta:**
```json
{
  "message": "Resumen enviado exitosamente",
  "taskCount": 12
}
```

### `POST /api/tasks`
Crea una tarea (envía WhatsApp automáticamente si está habilitado).

**Request:**
```json
{
  "title": "Verificar factura FAC-001",
  "description": "Revisar datos del proveedor",
  "priority": 2,  // 0=Low, 1=Medium, 2=High, 3=Urgent
  "dueDate": "2024-12-25T00:00:00Z"
}
```

---

## 🎨 Personalización

### Emojis por Prioridad
- 🔴 **Urgente** (Priority = 3)
- 🟠 **Alta** (Priority = 2)
- 🟡 **Media** (Priority = 1)
- 🟢 **Baja** (Priority = 0)

### Formato de Mensajes
Los mensajes usan formato de WhatsApp:
- `*texto*` = **negrita**
- `_texto_` = _cursiva_
- `~texto~` = ~tachado~

---

## 🔄 Integración con Otros Módulos

### Dashboard
Las tareas del dashboard automáticamente envían notificaciones cuando:
- Factura con proveedor desconocido
- Confianza de extracción < 70%
- Transacción sin reconciliar (próximamente)

### OneDrive Sync
Cuando hay errores en la sincronización:
- Se registra en logs
- Se crea tarea
- **¡Recibes WhatsApp!**

### Facturas Recibidas
Al procesar facturas:
- Extracción con IA
- Validación automática
- Si hay problemas → Tarea + WhatsApp

---

## 💡 Mejores Prácticas

### 1. Horarios de Notificación
Considera configurar horarios para evitar notificaciones nocturnas:
```csharp
// Próximamente: filtro por horario
if (DateTime.Now.Hour >= 8 && DateTime.Now.Hour <= 20)
{
    await _whatsAppService.SendTaskNotificationAsync(task);
}
```

### 2. Agrupar Notificaciones
Para evitar spam, agrupa notificaciones similares:
```csharp
// Próximamente: batch notifications
var tasks = GetTasksInLastHour();
if (tasks.Count > 5)
{
    await _whatsAppService.SendTasksSummaryAsync(tasks);
}
```

### 3. Prioridades
Configura para solo recibir tareas urgentes/altas:
```json
{
  "WhatsApp": {
    "MinimumPriority": 2  // Solo High y Urgent
  }
}
```

---

## 🆕 Próximas Funcionalidades

### En Desarrollo
- [ ] Resumen diario automático
- [ ] Múltiples destinatarios
- [ ] Notificaciones por tipo de tarea
- [ ] Respuestas interactivas (marcar como completado desde WhatsApp)
- [ ] Configuración desde el frontend
- [ ] Filtros por horario

### Ideas Futuras
- [ ] Integración con WhatsApp Business API (plantillas aprobadas)
- [ ] Estadísticas de notificaciones
- [ ] Notificaciones de reconciliación bancaria
- [ ] Alertas de facturas próximas a vencer

---

## 📊 Límites y Costos

### Tier Gratuito de Meta
- **1,000 conversaciones/mes**: GRATIS
- Una conversación = ventana de 24 horas
- Múltiples mensajes en la misma ventana = 1 conversación

### Ejemplo de Uso Típico
- 30 tareas/día = 900/mes → **GRATIS** ✅
- 100 tareas/día = 3,000/mes → ~$20 USD/mes
- Resumen diario = 30/mes → **GRATIS** ✅

**Conclusión**: Para la mayoría de usuarios, será completamente GRATIS.

---

## 🛡️ Seguridad

### Datos Sensibles
- Los tokens NUNCA se loguean
- Los números de teléfono se sanitizan
- No se envían datos confidenciales de facturas

### Privacidad
- Solo se envían resúmenes
- No se incluyen montos ni datos bancarios
- Cumple con políticas de WhatsApp Business

---

## 🐛 Troubleshooting

### No recibo notificaciones
1. ✅ Verifica `"Enabled": true`
2. ✅ Verifica que el token sea válido
3. ✅ Revisa logs: `dotnet run` muestra errores
4. ✅ Prueba con `/api/whatsapp/test`

### Mensajes fallan
1. ✅ Verifica Phone Number ID
2. ✅ Verifica formato del número (sin + ni espacios)
3. ✅ Asegúrate de que el número esté verificado en Meta

### Token expirado
Si usas Temporary token:
- Expira en 24h
- Genera uno nuevo en Meta
- O crea un token permanente (ver guía completa)

---

## 📚 Documentación Adicional

- [Guía Completa de Configuración](./WHATSAPP_SETUP.md) - Configuración paso a paso
- [Inicio Rápido](./WHATSAPP_QUICK_START.md) - Setup en 5 minutos

---

¿Preguntas? ¿Sugerencias? Abre un issue en el repositorio. 🚀

