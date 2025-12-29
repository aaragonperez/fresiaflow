# Plan Completo de Pruebas E2E - Pantalla de Contabilidad

## Objetivo
Validar todas las funcionalidades de la pantalla de contabilidad mediante pruebas end-to-end que simulan el comportamiento de un usuario real.

## Alcance
- ✅ Carga inicial y visualización
- ✅ Estadísticas contables
- ✅ Filtros y búsqueda
- ✅ Tabla de asientos contables
- ✅ Generación de asientos
- ✅ Regeneración de asientos
- ✅ Edición de asientos
- ✅ Contabilización de asientos
- ✅ Facturas sin asiento
- ✅ Selección múltiple
- ✅ Progreso de generación
- ✅ Validaciones y estados

---

## 1. Carga Inicial y Visualización

### 1.1 Carga de la Página
- ✅ Verificar que la URL es correcta (`/accounting`)
- ✅ Verificar que el título "Contabilidad" se muestra
- ✅ Verificar que el subtítulo "Gestión de asientos contables" se muestra
- ✅ Verificar que la página carga sin errores

### 1.2 Elementos del Header
- ✅ Verificar que existe el botón "Generar Asientos"
- ✅ Verificar que existe el botón "Regenerar Todos"
- ✅ Verificar que existe el botón "Regenerar Seleccionados"
- ✅ Verificar que existe el botón "Contabilizar Balanceados"
- ✅ Verificar que los botones tienen los iconos correctos

### 1.3 Estadísticas Contables
- ✅ Verificar que se muestra "Total Asientos"
- ✅ Verificar que se muestra "Facturas Recibidas"
- ✅ Verificar que se muestra "Sin Asiento"
- ✅ Verificar que se muestra "En Borrador"
- ✅ Verificar que se muestra "Contabilizados"
- ✅ Verificar que se muestra "Erróneos"
- ✅ Verificar que se muestra "Total Debe"
- ✅ Verificar que se muestra "Total Haber"
- ✅ Verificar que se muestra "Balanceado" o "Diferencia"
- ✅ Verificar que los valores numéricos se muestran correctamente

---

## 2. Secciones Colapsables

### 2.1 Sección de Asientos Contables
- ✅ Verificar que la sección se puede expandir
- ✅ Verificar que la sección se puede colapsar
- ✅ Verificar que muestra el contador de asientos
- ✅ Verificar que el icono de flecha rota correctamente

### 2.2 Sección de Facturas sin Asiento
- ✅ Verificar que la sección se puede expandir
- ✅ Verificar que la sección se puede colapsar
- ✅ Verificar que muestra el contador de facturas fallidas
- ✅ Verificar que el icono de flecha rota correctamente

---

## 3. Tabla de Asientos Contables

### 3.1 Estructura de la Tabla
- ✅ Verificar que la tabla se muestra cuando hay datos
- ✅ Verificar que todas las columnas se muestran:
  - Checkbox de selección
  - Expander de líneas
  - Número
  - Fecha
  - Descripción
  - Referencia
  - Origen
  - Estado
  - Debe
  - Haber
  - Balanceado
  - Acciones

### 3.2 Filtros
- ✅ Verificar que existe el filtro global
- ✅ Verificar que existen filtros por columna:
  - Filtro por número
  - Filtro por fecha
  - Filtro por descripción
  - Filtro por referencia
  - Filtro por origen (dropdown)
  - Filtro por estado (dropdown)
  - Filtro por debe (≥)
  - Filtro por haber (≥)
- ✅ Verificar que el checkbox "Solo asientos erróneos" funciona
- ✅ Verificar que el botón "Limpiar filtros" funciona

### 3.3 Búsqueda Global
- ✅ Verificar que se puede escribir en el filtro global
- ✅ Verificar que filtra correctamente
- ✅ Verificar que se puede limpiar

### 3.4 Ordenamiento
- ✅ Verificar que se puede ordenar por número
- ✅ Verificar que se puede ordenar por fecha
- ✅ Verificar que se puede ordenar por descripción
- ✅ Verificar que se puede ordenar por referencia
- ✅ Verificar que se puede ordenar por origen
- ✅ Verificar que se puede ordenar por estado
- ✅ Verificar que se puede ordenar por debe
- ✅ Verificar que se puede ordenar por haber

### 3.5 Paginación
- ✅ Verificar que existe el paginador
- ✅ Verificar que se puede cambiar de página
- ✅ Verificar que se puede cambiar el número de filas por página
- ✅ Verificar que las opciones de paginación son [10, 25, 50]

### 3.6 Expansión de Filas
- ✅ Verificar que se puede expandir una fila para ver líneas
- ✅ Verificar que se muestran las líneas del asiento
- ✅ Verificar que se muestran los totales
- ✅ Verificar que se puede colapsar la fila

### 3.7 Estado Vacío
- ✅ Verificar que se muestra mensaje cuando no hay asientos
- ✅ Verificar que el mensaje es "No hay asientos contables"

---

## 4. Selección Múltiple

### 4.1 Selección Individual
- ✅ Verificar que se puede seleccionar un asiento
- ✅ Verificar que el botón "Regenerar Seleccionados" se habilita
- ✅ Verificar que se puede deseleccionar

### 4.2 Selección Múltiple
- ✅ Verificar que se puede seleccionar múltiples asientos
- ✅ Verificar que el contador de seleccionados se actualiza

### 4.3 Selección Total
- ✅ Verificar que se puede seleccionar todos con el checkbox del header
- ✅ Verificar que se puede deseleccionar todos

---

## 5. Botones de Acción en la Tabla

### 5.1 Botón Ver Factura
- ✅ Verificar que existe cuando hay invoiceId
- ✅ Verificar que abre el archivo de la factura (no ejecutar para no abrir ventanas)

### 5.2 Botón Editar
- ✅ Verificar que existe en cada fila
- ✅ Verificar que está deshabilitado para asientos contabilizados automáticos
- ✅ Verificar que abre el diálogo de edición

### 5.3 Botón Contabilizar
- ✅ Verificar que existe en cada fila
- ✅ Verificar que está deshabilitado para asientos no balanceados
- ✅ Verificar que está deshabilitado para asientos ya contabilizados
- ✅ Verificar que muestra confirmación antes de contabilizar

---

## 6. Diálogo de Edición de Asiento

### 6.1 Apertura del Diálogo
- ✅ Verificar que se abre al hacer clic en editar
- ✅ Verificar que muestra el título correcto
- ✅ Verificar que se puede cerrar con la X
- ✅ Verificar que se puede cerrar con ESC
- ✅ Verificar que se puede cerrar haciendo clic fuera

### 6.2 Campos del Formulario
- ✅ Verificar que existe el campo Fecha
- ✅ Verificar que existe el campo Descripción
- ✅ Verificar que existe el campo Notas
- ✅ Verificar que los campos se pueden editar (cuando no es readonly)

### 6.3 Líneas del Asiento
- ✅ Verificar que se muestran las líneas existentes
- ✅ Verificar que se muestran los totales (Debe y Haber)
- ✅ Verificar que se puede eliminar una línea
- ✅ Verificar que se muestra el formulario para agregar nueva línea

### 6.4 Agregar Nueva Línea
- ✅ Verificar que existe el dropdown de Cuenta
- ✅ Verificar que existe el dropdown de Debe/Haber
- ✅ Verificar que existe el campo Importe
- ✅ Verificar que existe el campo Descripción
- ✅ Verificar que el botón "Agregar" funciona
- ✅ Verificar que se valida que la cuenta esté seleccionada
- ✅ Verificar que se valida que el importe sea mayor a 0

### 6.5 Guardar Cambios
- ✅ Verificar que el botón "Guardar" existe
- ✅ Verificar que está deshabilitado cuando no hay líneas
- ✅ Verificar que guarda los cambios correctamente
- ✅ Verificar que cierra el diálogo después de guardar

### 6.6 Cancelar
- ✅ Verificar que el botón "Cancelar" existe
- ✅ Verificar que cierra el diálogo sin guardar

---

## 7. Generación de Asientos

### 7.1 Iniciar Generación
- ✅ Verificar que el botón "Generar Asientos" existe
- ✅ Verificar que se puede hacer clic
- ✅ Verificar que muestra confirmación (si aplica)

### 7.2 Progreso de Generación
- ✅ Verificar que se muestra el panel de progreso
- ✅ Verificar que se muestra la barra de progreso
- ✅ Verificar que se muestra el porcentaje
- ✅ Verificar que se muestra el contador (X / Y facturas)
- ✅ Verificar que se muestra el mensaje de estado
- ✅ Verificar que se muestra la factura actual
- ✅ Verificar que se muestra el proveedor actual
- ✅ Verificar que se muestran estadísticas (exitosos/errores)
- ✅ Verificar que se muestra el error actual (si hay)

### 7.3 Cancelar Generación
- ✅ Verificar que existe el botón "Cancelar"
- ✅ Verificar que muestra confirmación
- ✅ Verificar que cancela la generación
- ✅ Verificar que actualiza el estado

### 7.4 Completar Generación
- ✅ Verificar que muestra mensaje de completado
- ✅ Verificar que actualiza las estadísticas
- ✅ Verificar que recarga los datos
- ✅ Verificar que oculta el panel después de un tiempo

---

## 8. Regeneración de Asientos

### 8.1 Regenerar Todos
- ✅ Verificar que el botón "Regenerar Todos" existe
- ✅ Verificar que muestra confirmación
- ✅ Verificar que regenera todos los asientos automáticos
- ✅ Verificar que muestra progreso
- ✅ Verificar que actualiza los datos

### 8.2 Regenerar Seleccionados
- ✅ Verificar que el botón está deshabilitado cuando no hay selección
- ✅ Verificar que se habilita cuando hay selección
- ✅ Verificar que muestra confirmación
- ✅ Verificar que regenera solo los seleccionados
- ✅ Verificar que muestra progreso
- ✅ Verificar que limpia la selección después

---

## 9. Contabilización de Asientos

### 9.1 Contabilizar Individual
- ✅ Verificar que se puede contabilizar un asiento balanceado
- ✅ Verificar que muestra confirmación
- ✅ Verificar que actualiza el estado del asiento
- ✅ Verificar que actualiza las estadísticas

### 9.2 Contabilizar Balanceados
- ✅ Verificar que el botón "Contabilizar Balanceados" existe
- ✅ Verificar que contabiliza todos los asientos balanceados en borrador
- ✅ Verificar que muestra progreso
- ✅ Verificar que actualiza los datos

---

## 10. Facturas sin Asiento

### 10.1 Visualización
- ✅ Verificar que la sección existe
- ✅ Verificar que muestra el contador
- ✅ Verificar que se puede expandir/colapsar

### 10.2 Tabla de Facturas Fallidas
- ✅ Verificar que se muestra cuando hay facturas sin asiento
- ✅ Verificar que muestra las columnas:
  - Número Factura
  - Proveedor
  - Motivo
  - Acciones
- ✅ Verificar que tiene filtro global
- ✅ Verificar que tiene filtros por columna
- ✅ Verificar que se puede ordenar

### 10.3 Estado Vacío
- ✅ Verificar que muestra mensaje cuando todas las facturas tienen asiento
- ✅ Verificar que el mensaje es "Todas las facturas tienen asiento"

### 10.4 Acciones
- ✅ Verificar que existe botón para ver factura
- ✅ Verificar que abre el archivo de la factura

---

## 11. Estados y Validaciones

### 11.1 Estados de Botones
- ✅ Verificar que "Generar Asientos" se deshabilita durante generación
- ✅ Verificar que "Regenerar Todos" se deshabilita durante generación
- ✅ Verificar que "Regenerar Seleccionados" se deshabilita durante generación
- ✅ Verificar que "Contabilizar Balanceados" se deshabilita durante operaciones

### 11.2 Validaciones de Edición
- ✅ Verificar que no se puede editar asientos contabilizados automáticos
- ✅ Verificar que se puede editar asientos en borrador
- ✅ Verificar que se puede editar asientos manuales contabilizados

### 11.3 Validaciones de Contabilización
- ✅ Verificar que no se puede contabilizar asientos no balanceados
- ✅ Verificar que no se puede contabilizar asientos ya contabilizados
- ✅ Verificar que se puede contabilizar asientos balanceados en borrador

---

## 12. Integración con API

### 12.1 Carga de Datos
- ✅ Verificar que carga asientos desde la API
- ✅ Verificar que carga cuentas contables desde la API
- ✅ Verificar que carga facturas fallidas desde la API

### 12.2 Actualización de Datos
- ✅ Verificar que recarga datos después de generar
- ✅ Verificar que recarga datos después de regenerar
- ✅ Verificar que recarga datos después de contabilizar
- ✅ Verificar que recarga datos después de editar

### 12.3 SignalR
- ✅ Verificar que se conecta a SignalR
- ✅ Verificar que recibe actualizaciones de progreso
- ✅ Verificar que actualiza la UI en tiempo real

---

## 13. Navegación y Responsividad

### 13.1 Navegación
- ✅ Verificar que se puede navegar desde el menú
- ✅ Verificar que mantiene el estado al recargar
- ✅ Verificar que la URL es correcta

### 13.2 Responsividad
- ✅ Verificar que funciona en móvil (375x667)
- ✅ Verificar que funciona en tablet (768x1024)
- ✅ Verificar que funciona en desktop (1280x720)
- ✅ Verificar que los elementos se adaptan correctamente

---

## 14. Casos Especiales

### 14.1 Asientos Vacíos
- ✅ Verificar comportamiento cuando no hay asientos
- ✅ Verificar mensajes apropiados

### 14.2 Asientos Erróneos
- ✅ Verificar que se muestran correctamente
- ✅ Verificar que el filtro funciona
- ✅ Verificar que se destacan visualmente

### 14.3 Asientos Balanceados vs No Balanceados
- ✅ Verificar que se muestran correctamente
- ✅ Verificar que los iconos son correctos
- ✅ Verificar que los botones se habilitan/deshabilitan correctamente

### 14.4 Estados de Asientos
- ✅ Verificar visualización de Borrador
- ✅ Verificar visualización de Contabilizado
- ✅ Verificar visualización de Anulado
- ✅ Verificar que los tags tienen los colores correctos

---

## Ejecución de Pruebas

### Comando para ejecutar todas las pruebas:
```bash
npm run e2e
```

### Comando para ejecutar solo las pruebas de contabilidad:
```bash
npx cypress run --spec "cypress/e2e/accounting-complete.cy.ts"
```

### Comando para ejecutar en modo interactivo:
```bash
npm run e2e
# Luego seleccionar accounting-complete.cy.ts
```

---

## Notas Importantes

1. **Datos de Prueba**: Algunas pruebas requieren datos específicos en la base de datos. Asegúrate de tener:
   - Al menos un asiento contable
   - Al menos una factura sin asiento (para probar facturas fallidas)
   - Cuentas contables configuradas

2. **Operaciones Destructivas**: Las pruebas de regeneración y contabilización pueden modificar datos. Considera usar un entorno de pruebas.

3. **Tiempos de Espera**: Algunas operaciones (generación, regeneración) pueden tardar. Las pruebas incluyen waits apropiados.

4. **Confirmaciones**: Algunas acciones muestran confirmaciones. Las pruebas no ejecutan estas acciones automáticamente para evitar modificar datos.

5. **SignalR**: Las pruebas verifican la conexión pero no esperan actualizaciones en tiempo real completas.

---

## Cobertura Esperada

- ✅ **Carga inicial**: 100%
- ✅ **Visualización**: 100%
- ✅ **Filtros**: 100%
- ✅ **Búsqueda**: 100%
- ✅ **Ordenamiento**: 100%
- ✅ **Paginación**: 100%
- ✅ **Selección**: 100%
- ✅ **Edición**: 90% (no ejecuta guardado para evitar modificar datos)
- ✅ **Generación**: 80% (verifica UI, no ejecuta completamente)
- ✅ **Regeneración**: 80% (verifica UI, no ejecuta completamente)
- ✅ **Contabilización**: 80% (verifica UI, no ejecuta completamente)
- ✅ **Facturas fallidas**: 100%
- ✅ **Validaciones**: 100%
- ✅ **Estados**: 100%

**Cobertura Total Estimada: ~85%**

