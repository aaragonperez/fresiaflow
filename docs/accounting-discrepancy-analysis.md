# Análisis de Discrepancia entre Facturas y Contabilidad

## Problema
- **Facturas**: 1441 facturas con total facturado de 185,428€
- **Contabilidad**: El total no coincide

## Estructura de un Asiento Contable Generado

### Caso 1: Factura SIN IRPF
```
DEBE:
  - Base Imponible (SubtotalAmount)
  - IVA (TaxAmount, si existe)

HABER:
  - Proveedores (TotalAmount)
```

**Total HABER = TotalAmount de la factura** ✅

### Caso 2: Factura CON IRPF
```
DEBE:
  - Base Imponible (SubtotalAmount)
  - IVA (TaxAmount, si existe)

HABER:
  - Proveedores (TotalAmount - IrpfAmount)
  - IRPF (IrpfAmount)
```

**Total HABER = (TotalAmount - IrpfAmount) + IrpfAmount = TotalAmount** ✅

## Posibles Causas de la Discrepancia

### 1. **Facturas sin Asientos Generados**
- El sistema solo genera asientos para facturas que **NO tienen asiento previo**
- Si una factura ya tiene asiento, se omite (línea 90-109 del código)
- **Solución**: Verificar cuántas facturas tienen asientos vs cuántas no

### 2. **Facturas con Errores al Generar Asientos**
- Si una factura falla al generar el asiento (por ejemplo, no está balanceada), se registra como error pero no se crea el asiento
- **Solución**: Revisar los errores reportados durante la generación

### 3. **Facturas con Importes Cero o Negativos**
- Las facturas con importes cero o negativos pueden no generar asientos correctamente
- **Solución**: Verificar si hay facturas con estos casos especiales

### 4. **Cálculo del Total en Contabilidad**
- El total en contabilidad suma `totalCredit` de todos los asientos
- Esto debería ser igual a la suma de `TotalAmount` de todas las facturas con asientos
- **Problema**: Si no todas las facturas tienen asientos, el total será menor

## Cómo Verificar

1. **Contar asientos vs facturas**:
   - Total de asientos generados
   - Total de facturas
   - Diferencia = facturas sin asientos

2. **Sumar totalCredit de asientos**:
   - Debería ser igual a la suma de TotalAmount de las facturas que tienen asientos

3. **Verificar facturas sin asientos**:
   - Buscar facturas que no tienen `InvoiceId` asociado a ningún asiento

## Recomendación

El problema más probable es que **no todas las facturas tienen asientos generados**. Esto puede deberse a:
- Facturas que ya tenían asientos y se omitieron
- Facturas que fallaron al generar asientos
- Facturas que no se procesaron por algún error

**Solución**: Regenerar todos los asientos para asegurar que todas las facturas tengan su asiento correspondiente.

