# AUDITORÍA COMPLETA: SISTEMA DE FACTURAS RECIBIDAS

## 1. ESTRUCTURA ESPERADA SEGÚN PROMPT DE OPENAI

### Prompt: `CompleteExtractionTemplate`
```json
{
  "invoiceNumber": "string",
  "supplierName": "string",
  "supplierTaxId": "string o null",
  "issueDate": "YYYY-MM-DD",
  "dueDate": "YYYY-MM-DD o null",
  "totalAmount": number,
  "taxAmount": number o null,
  "subtotalAmount": number o null,
  "currency": "EUR",
  "lines": [
    {
      "lineNumber": 1,
      "description": "string",
      "quantity": number,
      "unitPrice": number,
      "taxRate": number o null,
      "lineTotal": number
    }
  ]
}
```

---

## 2. ANÁLISIS POR CAPA

### 2.1 DOMINIO

#### Entidad `InvoiceReceived` (✅ COMPLETA)
- ✅ invoiceNumber
- ✅ supplierName
- ✅ supplierTaxId
- ✅ issueDate
- ✅ dueDate
- ✅ totalAmount (Money)
- ✅ taxAmount (Money?)
- ✅ subtotalAmount (Money?)
- ✅ currency
- ✅ lines (InvoiceReceivedLine[])
- ✅ Campos adicionales: OriginalFilePath, ProcessedFilePath, ProcessedAt, Status, Notes

#### Entidad `Invoice` (❌ INCOMPLETA - PROBLEMA CRÍTICO)
- ✅ invoiceNumber
- ✅ issueDate
- ✅ dueDate
- ✅ amount (Money) - pero NO tiene totalAmount, taxAmount, subtotalAmount separados
- ✅ supplierName
- ❌ supplierTaxId - FALTA
- ❌ taxAmount - FALTA
- ❌ subtotalAmount - FALTA
- ❌ currency explícito (solo en Money)
- ❌ lines - FALTA COMPLETAMENTE
- ⚠️ Campos adicionales: Status, FilePath, CreatedAt, ReconciledAt (no del prompt)

**PROBLEMA**: Hay DOS entidades diferentes para facturas recibidas:
- `Invoice` - usada en UploadInvoiceUseCase (BÁSICA)
- `InvoiceReceived` - usada en ProcessIncomingInvoiceCommandHandler (COMPLETA)

---

### 2.2 PERSISTENCIA

#### Tabla `InvoicesReceived` (✅ COMPLETA)
- Todos los campos del prompt están mapeados correctamente
- Money como Owned Types con precisión adecuada
- Líneas en tabla separada `InvoiceReceivedLines`

#### Tabla `Invoices` (❌ INCOMPLETA)
- Solo campos básicos
- NO tiene supplierTaxId
- NO tiene taxAmount, subtotalAmount separados
- NO tiene líneas de detalle

---

### 2.3 DTOs Y MAPEOS

#### `InvoiceExtractionResultDto` (✅ COMPLETA)
- Todos los campos del prompt están presentes
- Incluye `InvoiceExtractionLineDto[]`

#### `UploadInvoiceUseCase.InvoiceExtractionResult` (❌ INCOMPLETA)
- Solo tiene: InvoiceNumber, IssueDate, DueDate, Amount, SupplierName, Confidence
- ❌ FALTA: supplierTaxId, taxAmount, subtotalAmount, currency, lines

#### Mapeo en `InvoicesController` (❌ INCOMPLETA)
- Devuelve entidad `Invoice` directamente (no DTO)
- Solo expone campos básicos

#### Mapeo en `InvoiceReceivedController` (✅ COMPLETA)
- Mapea correctamente todos los campos de `InvoiceReceived`
- Incluye líneas de detalle

---

### 2.4 API ENDPOINTS

#### `/api/invoices` (❌ PROBLEMA)
- Usa `Invoice` (entidad básica)
- Solo devuelve campos básicos
- NO expone: supplierTaxId, taxAmount, subtotalAmount, lines

#### `/api/invoices/received` (✅ CORRECTO)
- Usa `InvoiceReceived` (entidad completa)
- Expone todos los campos del prompt
- Incluye líneas de detalle

**PROBLEMA**: Hay DOS endpoints diferentes para facturas recibidas:
- `/api/invoices` - devuelve `Invoice` (básico)
- `/api/invoices/received` - devuelve `InvoiceReceived` (completo)

---

### 2.5 FRONTEND (ANGULAR)

#### Modelo `Invoice` (❌ INCOMPLETA)
```typescript
{
  id, invoiceNumber, issueDate, dueDate, amount, status, 
  supplierName, filePath, createdAt, reconciledAt
}
```
- ❌ FALTA: supplierTaxId
- ❌ FALTA: taxAmount
- ❌ FALTA: subtotalAmount
- ❌ FALTA: currency (solo dentro de amount)
- ❌ FALTA: lines

#### Componente `InvoiceTableComponent` (❌ INCOMPLETA)
- Solo muestra: número, proveedor, fechas, monto, estado
- ❌ NO muestra: supplierTaxId, taxAmount, subtotalAmount, líneas

#### Servicio `InvoiceHttpAdapter` (❌ PROBLEMA)
- Consume `/api/invoices` (endpoint básico)
- NO consume `/api/invoices/received` (endpoint completo)

---

## 3. PROBLEMAS CRÍTICOS IDENTIFICADOS

### PROBLEMA #1: DUALIDAD DE ENTIDADES
- `Invoice` y `InvoiceReceived` representan lo mismo pero con diferentes niveles de detalle
- `UploadInvoiceUseCase` usa `Invoice` (básica)
- `ProcessIncomingInvoiceCommandHandler` usa `InvoiceReceived` (completa)
- **IMPACTO**: Datos extraídos por OpenAI se pierden en UploadInvoiceUseCase

### PROBLEMA #2: PROMPT INCORRECTO EN UPLOAD
- `UploadInvoiceUseCase` usa `BasicExtractionTemplate` (solo 6 campos)
- Debería usar `CompleteExtractionTemplate` (todos los campos + líneas)
- **IMPACTO**: Se extrae menos información de la disponible

### PROBLEMA #3: PÉRDIDA DE DATOS EN PERSISTENCIA
- `UploadInvoiceUseCase` persiste en tabla `Invoices` (básica)
- No persiste: supplierTaxId, taxAmount, subtotalAmount, lines
- **IMPACTO**: Información fiscal y de detalle se pierde

### PROBLEMA #4: API INCONSISTENTE
- Dos endpoints diferentes para el mismo concepto
- Frontend consume el endpoint básico
- **IMPACTO**: UI no puede mostrar información completa

### PROBLEMA #5: UI INCOMPLETA
- Modelo TypeScript no tiene campos completos
- Componente solo muestra 6 campos básicos
- **IMPACTO**: Usuario no ve información fiscal ni líneas de detalle

---

## 4. CAMPOS FALTANTES POR CAPA

### En `Invoice` (vs prompt):
- ❌ supplierTaxId
- ❌ taxAmount (separado)
- ❌ subtotalAmount (separado)
- ❌ currency (explícito)
- ❌ lines[] (completo)

### En DTO de `UploadInvoiceUseCase`:
- ❌ supplierTaxId
- ❌ taxAmount
- ❌ subtotalAmount
- ❌ currency
- ❌ lines[]

### En API `/api/invoices`:
- ❌ supplierTaxId
- ❌ taxAmount
- ❌ subtotalAmount
- ❌ lines[]

### En Frontend:
- ❌ supplierTaxId
- ❌ taxAmount
- ❌ subtotalAmount
- ❌ lines[]

---

## 5. DECISIONES ARQUITECTÓNICAS REQUERIDAS

### DECISIÓN #1: ¿Qué entidad usar?
**RECOMENDACIÓN**: Unificar en `InvoiceReceived` y eliminar `Invoice` para facturas recibidas.
- `Invoice` puede mantenerse para facturas emitidas (IssuedInvoice ya existe)
- O renombrar: `InvoiceReceived` → `Invoice` y `Invoice` actual → `InvoiceReceivedBasic` (deprecar)

### DECISIÓN #2: ¿Qué prompt usar?
**RECOMENDACIÓN**: `UploadInvoiceUseCase` debe usar `CompleteExtractionTemplate`
- Cambiar de BasicExtractionTemplate a CompleteExtractionTemplate
- Actualizar InvoiceExtractionResult para incluir todos los campos

### DECISIÓN #3: ¿Qué endpoint usar?
**RECOMENDACIÓN**: Unificar en `/api/invoices/received` o hacer que `/api/invoices` devuelva datos completos
- Frontend debe consumir el endpoint completo
- O migrar UploadInvoiceUseCase a usar InvoiceReceived

---

## 6. PLAN DE CORRECCIÓN

1. ✅ AUDITORÍA (COMPLETADA)
2. ⏳ Unificar entidades: Migrar UploadInvoiceUseCase a usar InvoiceReceived
3. ⏳ Cambiar prompt en UploadInvoiceUseCase a CompleteExtractionTemplate
4. ⏳ Actualizar mapeos y DTOs
5. ⏳ Actualizar API para devolver datos completos
6. ⏳ Actualizar Frontend con modelo completo y UI detallada
7. ⏳ Validaciones y consistencia

