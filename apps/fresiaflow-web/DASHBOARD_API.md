# API del Dashboard - Especificación de Endpoints

Este documento describe los endpoints que el backend debe implementar para que el dashboard funcione correctamente.

## Endpoints Requeridos

### 1. GET /api/dashboard/tasks

Obtiene todas las tareas pendientes del dashboard.

**Respuesta esperada:**
```json
[
  {
    "id": "string (UUID)",
    "title": "string",
    "description": "string (opcional)",
    "type": "invoice | bank | supplier | system | review",
    "priority": "high | medium | low",
    "status": "pending | in_progress | completed | cancelled",
    "dueDate": "YYYY-MM-DDTHH:mm:ssZ (opcional)",
    "createdAt": "YYYY-MM-DDTHH:mm:ssZ",
    "updatedAt": "YYYY-MM-DDTHH:mm:ssZ",
    "metadata": {} // objeto opcional con datos adicionales
  }
]
```

**Notas:**
- Las tareas deben estar ordenadas por prioridad (alta primero) y luego por fecha límite.
- El campo `metadata` puede contener información específica del tipo de tarea (ej: `invoiceId`, `bankAccountId`, etc.).

---

### 2. GET /api/dashboard/bank-balances

Obtiene el resumen de saldos bancarios.

**Respuesta esperada:**
```json
{
  "banks": [
    {
      "bankId": "string (UUID)",
      "bankName": "string",
      "accountNumber": "string (opcional)",
      "balance": 1234.56,
      "currency": "EUR",
      "lastMovementDate": "YYYY-MM-DDTHH:mm:ssZ (opcional)",
      "lastMovementAmount": 100.00 // opcional, puede ser negativo
    }
  ],
  "totalBalance": 1234.56,
  "primaryCurrency": "EUR",
  "previousDayBalance": 1200.00, // opcional
  "previousDayVariation": 2.88, // opcional, porcentaje
  "previousMonthBalance": 1000.00, // opcional
  "previousMonthVariation": 23.46 // opcional, porcentaje
}
```

**Notas:**
- `totalBalance` es la suma de todos los saldos convertidos a la moneda primaria.
- Las variaciones son porcentajes (positivo = aumento, negativo = disminución).
- Si no hay datos históricos, los campos `previousDay*` y `previousMonth*` pueden omitirse.

---

### 3. GET /api/dashboard/alerts

Obtiene todas las alertas activas.

**Respuesta esperada:**
```json
[
  {
    "id": "string (UUID)",
    "type": "unusual_movement | duplicate_amount | unidentified_charge | pattern_deviation | missing_supplier | overdue_invoice | low_balance | system",
    "severity": "critical | high | medium | low | info",
    "title": "string",
    "description": "string",
    "occurredAt": "YYYY-MM-DDTHH:mm:ssZ",
    "acknowledgedAt": "YYYY-MM-DDTHH:mm:ssZ (opcional)",
    "resolvedAt": "YYYY-MM-DDTHH:mm:ssZ (opcional)",
    "metadata": {} // objeto opcional con datos adicionales
  }
]
```

**Notas:**
- Las alertas deben estar ordenadas por severidad (crítica primero) y luego por fecha (más recientes primero).
- Una alerta resuelta (`resolvedAt` presente) puede seguir apareciendo en la lista, pero el frontend la mostrará con menor opacidad.
- El campo `metadata` puede contener información específica del tipo de alerta (ej: `transactionId`, `amount`, `invoiceId`, etc.).

---

## Implementación Temporal

Si los endpoints aún no están implementados en el backend, el frontend:
- Manejará el error 404 de forma silenciosa
- Mostrará estados vacíos en los widgets
- Registrará un warning en la consola del navegador

Esto permite que el dashboard se cargue correctamente mientras se implementan los endpoints.

---

## Suposiciones y Consideraciones

1. **Tareas**: Se asume que las tareas pueden generarse automáticamente por:
   - Facturas pendientes de revisión
   - Movimientos bancarios sin conciliar
   - Proveedores sin datos completos
   - Tareas del sistema (backups, actualizaciones, etc.)

2. **Saldos Bancarios**: Se asume que:
   - Los bancos están conectados mediante integraciones (Open Banking, APIs, etc.)
   - Los saldos se actualizan periódicamente
   - Se mantiene un historial para calcular variaciones

3. **Alertas**: Se asume que:
   - Las alertas se generan mediante reglas de negocio en el backend
   - Las alertas pueden ser reconocidas (`acknowledgedAt`) y resueltas (`resolvedAt`)
   - El backend decide qué es "inusual" o "sospechoso"

---

## Próximos Pasos (Futuro)

- Endpoint para marcar tareas como completadas: `PATCH /api/dashboard/tasks/{id}/complete`
- Endpoint para reconocer alertas: `PATCH /api/dashboard/alerts/{id}/acknowledge`
- Endpoint para resolver alertas: `PATCH /api/dashboard/alerts/{id}/resolve`
- WebSocket o SignalR para actualizaciones en tiempo real

