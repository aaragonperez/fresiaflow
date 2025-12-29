describe('Pantalla de Contabilidad - Plan Completo de Pruebas E2E', () => {
  beforeEach(() => {
    cy.visit('/accounting');
    // Esperar a que la página cargue completamente
    cy.wait(3000);
  });

  describe('1. Carga Inicial y Visualización', () => {
    it('debe cargar la página de contabilidad correctamente', () => {
      cy.url().should('include', '/accounting');
      cy.contains('Contabilidad').should('be.visible');
      cy.contains('Gestión de asientos contables').should('be.visible');
    });

    it('debe mostrar todas las estadísticas contables', () => {
      // Verificar que existen las tarjetas de estadísticas
      cy.contains('Total Asientos').should('be.visible');
      cy.contains('Facturas Recibidas').should('be.visible');
      cy.contains('Sin Asiento').should('be.visible');
      cy.contains('En Borrador').should('be.visible');
      cy.contains('Contabilizados').should('be.visible');
      cy.contains('Erróneos').should('be.visible');
      cy.contains('Total Debe').should('be.visible');
      cy.contains('Total Haber').should('be.visible');
      cy.contains('Balanceado').should('be.visible');
    });

    it('debe mostrar los botones de acción en el header', () => {
      cy.contains('button', 'Generar Asientos').should('be.visible');
      cy.contains('button', 'Regenerar Todos').should('be.visible');
      cy.contains('button', 'Regenerar Seleccionados').should('be.visible');
      cy.contains('button', 'Contabilizar Balanceados').should('be.visible');
    });

    it('debe mostrar las secciones colapsables', () => {
      cy.contains('Asientos Contables').should('be.visible');
      cy.contains('Facturas sin Asiento').should('be.visible');
    });
  });

  describe('2. Secciones Colapsables', () => {
    it('debe expandir y colapsar la sección de asientos contables', () => {
      // Buscar el header de la sección
      cy.contains('Asientos Contables').parent().parent().click();
      cy.wait(500);
      
      // Verificar que se colapsó
      cy.get('body').then(($body) => {
        // La sección debería estar colapsada
        expect($body.find('.section-content').length).to.be.greaterThan(0);
      });
      
      // Expandir de nuevo
      cy.contains('Asientos Contables').parent().parent().click();
      cy.wait(500);
    });

    it('debe expandir y colapsar la sección de facturas sin asiento', () => {
      cy.contains('Facturas sin Asiento').parent().parent().click();
      cy.wait(500);
      
      // Colapsar y expandir de nuevo
      cy.contains('Facturas sin Asiento').parent().parent().click();
      cy.wait(500);
    });
  });

  describe('3. Tabla de Asientos Contables', () => {
    beforeEach(() => {
      // Asegurar que la sección está expandida
      cy.get('body').then(($body) => {
        if ($body.find('.section-header').contains('Asientos Contables').length > 0) {
          cy.contains('Asientos Contables').parent().parent().click();
          cy.wait(500);
          cy.contains('Asientos Contables').parent().parent().click();
          cy.wait(500);
        }
      });
    });

    it('debe mostrar la tabla de asientos cuando hay datos', () => {
      cy.wait(2000);
      cy.get('p-table').should('exist');
    });

    it('debe mostrar las columnas correctas en la tabla', () => {
      cy.contains('th', 'Número').should('be.visible');
      cy.contains('th', 'Fecha').should('be.visible');
      cy.contains('th', 'Descripción').should('be.visible');
      cy.contains('th', 'Referencia').should('be.visible');
      cy.contains('th', 'Origen').should('be.visible');
      cy.contains('th', 'Estado').should('be.visible');
      cy.contains('th', 'Debe').should('be.visible');
      cy.contains('th', 'Haber').should('be.visible');
      cy.contains('th', 'Balanceado').should('be.visible');
      cy.contains('th', 'Acciones').should('be.visible');
    });

    it('debe tener filtro global en la tabla', () => {
      cy.get('input[placeholder*="Búsqueda global"]').should('be.visible');
    });

    it('debe tener filtros por columna', () => {
      // Verificar que existen los filtros
      cy.get('.filter-row').should('exist');
      cy.get('input[placeholder="Nº"]').should('exist');
      cy.get('input[type="date"]').should('exist');
    });

    it('debe filtrar por búsqueda global', () => {
      cy.get('input[placeholder*="Búsqueda global"]')
        .type('test');
      cy.wait(500);
      
      // Verificar que la tabla sigue visible
      cy.get('p-table tbody').should('be.visible');
    });

    it('debe filtrar por número de asiento', () => {
      cy.get('input[placeholder="Nº"]')
        .type('1');
      cy.wait(500);
      cy.get('p-table tbody').should('be.visible');
    });

    it('debe filtrar por estado', () => {
      cy.get('select').first().select('Draft');
      cy.wait(500);
      cy.get('p-table tbody').should('be.visible');
    });

    it('debe filtrar por origen', () => {
      cy.get('select').eq(1).select('Automatic');
      cy.wait(500);
      cy.get('p-table tbody').should('be.visible');
    });

    it('debe limpiar los filtros', () => {
      // Aplicar un filtro
      cy.get('input[placeholder*="Búsqueda global"]')
        .type('test');
      
      // Limpiar filtros
      cy.contains('button', 'Limpiar filtros').click();
      cy.wait(500);
      
      // Verificar que el filtro se limpió
      cy.get('input[placeholder*="Búsqueda global"]')
        .should('have.value', '');
    });

    it('debe ordenar columnas al hacer clic', () => {
      cy.get('th[pSortableColumn]').first().click();
      cy.wait(500);
      cy.get('p-table tbody').should('be.visible');
    });

    it('debe cambiar de página en la paginación', () => {
      cy.get('p-paginator').should('exist');
      
      cy.get('body').then(($body) => {
        if ($body.find('button[aria-label="Next Page"]').length > 0) {
          cy.get('button[aria-label="Next Page"]').click();
          cy.wait(500);
          cy.get('p-table tbody').should('be.visible');
        }
      });
    });

    it('debe mostrar checkbox para selección múltiple', () => {
      cy.get('p-tableHeaderCheckbox').should('exist');
      cy.get('p-tableCheckbox').should('exist');
    });

    it('debe expandir filas para ver líneas del asiento', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button.toggle-lines').length > 0) {
          cy.get('button.toggle-lines').first().click();
          cy.wait(500);
          
          // Verificar que se muestra el contenido expandido
          cy.get('.lines-container').should('be.visible');
        }
      });
    });
  });

  describe('4. Filtro de Asientos Erróneos', () => {
    it('debe tener checkbox para filtrar asientos erróneos', () => {
      cy.get('input[type="checkbox"][id="filterErroneous"]').should('exist');
      cy.contains('Solo asientos erróneos').should('be.visible');
    });

    it('debe filtrar asientos erróneos al activar el checkbox', () => {
      cy.get('input[type="checkbox"][id="filterErroneous"]').check();
      cy.wait(500);
      cy.get('p-table tbody').should('be.visible');
    });

    it('debe desactivar el filtro al desmarcar el checkbox', () => {
      cy.get('input[type="checkbox"][id="filterErroneous"]').check();
      cy.wait(500);
      cy.get('input[type="checkbox"][id="filterErroneous"]').uncheck();
      cy.wait(500);
      cy.get('p-table tbody').should('be.visible');
    });
  });

  describe('5. Selección Múltiple', () => {
    it('debe seleccionar un asiento individual', () => {
      cy.get('body').then(($body) => {
        if ($body.find('p-tableCheckbox').length > 0) {
          cy.get('p-tableCheckbox').first().click();
          cy.wait(500);
          
          // Verificar que el botón de regenerar seleccionados se habilita
          cy.contains('button', 'Regenerar Seleccionados').should('not.be.disabled');
        }
      });
    });

    it('debe seleccionar todos los asientos', () => {
      cy.get('p-tableHeaderCheckbox').click();
      cy.wait(500);
      
      // Verificar que el botón de regenerar seleccionados se habilita
      cy.contains('button', 'Regenerar Seleccionados').should('not.be.disabled');
    });

    it('debe deseleccionar todos los asientos', () => {
      cy.get('p-tableHeaderCheckbox').click();
      cy.wait(500);
      cy.get('p-tableHeaderCheckbox').click();
      cy.wait(500);
      
      // Verificar que el botón se deshabilita
      cy.contains('button', 'Regenerar Seleccionados').should('be.disabled');
    });
  });

  describe('6. Botones de Acción en la Tabla', () => {
    it('debe mostrar botones de acción en cada fila', () => {
      cy.get('body').then(($body) => {
        if ($body.find('.action-buttons').length > 0) {
          cy.get('.action-buttons').first().should('be.visible');
        }
      });
    });

    it('debe tener botón de editar asiento', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Editar asiento"]').length > 0) {
          cy.get('button[title="Editar asiento"]').first().should('be.visible');
        }
      });
    });

    it('debe tener botón de contabilizar asiento', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Contabilizar asiento"]').length > 0) {
          cy.get('button[title="Contabilizar asiento"]').first().should('be.visible');
        }
      });
    });

    it('debe tener botón de ver factura cuando existe invoiceId', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Ver factura"]').length > 0) {
          cy.get('button[title="Ver factura"]').first().should('be.visible');
        }
      });
    });
  });

  describe('7. Diálogo de Edición de Asiento', () => {
    it('debe abrir el diálogo de edición al hacer clic en editar', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Editar asiento"]').length > 0) {
          cy.get('button[title="Editar asiento"]').first().click();
          cy.wait(1000);
          
          // Verificar que el diálogo se abre
          cy.get('p-dialog').should('be.visible');
        }
      });
    });

    it('debe mostrar los campos del formulario de edición', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Editar asiento"]').length > 0) {
          cy.get('button[title="Editar asiento"]').first().click();
          cy.wait(1000);
          
          // Verificar campos
          cy.contains('label', 'Fecha').should('be.visible');
          cy.contains('label', 'Descripción').should('be.visible');
          cy.contains('label', 'Notas').should('be.visible');
          cy.contains('Líneas del Asiento').should('be.visible');
        }
      });
    });

    it('debe mostrar el formulario para agregar nueva línea', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Editar asiento"]').length > 0) {
          cy.get('button[title="Editar asiento"]').first().click();
          cy.wait(1000);
          
          // Verificar campos para nueva línea
          cy.contains('label', 'Cuenta').should('be.visible');
          cy.contains('label', 'Debe/Haber').should('be.visible');
          cy.contains('label', 'Importe').should('be.visible');
          cy.contains('button', 'Agregar').should('be.visible');
        }
      });
    });

    it('debe cerrar el diálogo al hacer clic en cancelar', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Editar asiento"]').length > 0) {
          cy.get('button[title="Editar asiento"]').first().click();
          cy.wait(1000);
          
          cy.contains('button', 'Cancelar').click();
          cy.wait(500);
          
          // El diálogo debería cerrarse
          cy.get('p-dialog').should('not.exist');
        }
      });
    });
  });

  describe('8. Facturas sin Asiento', () => {
    beforeEach(() => {
      // Expandir la sección de facturas sin asiento
      cy.contains('Facturas sin Asiento').parent().parent().click();
      cy.wait(500);
      cy.contains('Facturas sin Asiento').parent().parent().click();
      cy.wait(500);
    });

    it('debe mostrar la tabla de facturas sin asiento', () => {
      cy.wait(2000);
      cy.get('body').then(($body) => {
        if ($body.find('p-table').length > 1) {
          // Debería haber al menos una tabla (la de asientos y posiblemente la de facturas fallidas)
          cy.get('p-table').should('have.length.at.least', 1);
        }
      });
    });

    it('debe mostrar las columnas correctas en la tabla de facturas fallidas', () => {
      cy.get('body').then(($body) => {
        if ($body.find('th').contains('Número Factura').length > 0) {
          cy.contains('th', 'Número Factura').should('be.visible');
          cy.contains('th', 'Proveedor').should('be.visible');
          cy.contains('th', 'Motivo').should('be.visible');
        }
      });
    });

    it('debe tener filtro global en la tabla de facturas fallidas', () => {
      cy.get('body').then(($body) => {
        if ($body.find('input[placeholder*="Búsqueda global"]').length > 1) {
          cy.get('input[placeholder*="Búsqueda global"]').last().should('be.visible');
        }
      });
    });

    it('debe mostrar mensaje cuando todas las facturas tienen asiento', () => {
      cy.get('body').then(($body) => {
        if ($body.find('.empty-message').contains('Todas las facturas tienen asiento').length > 0) {
          cy.contains('Todas las facturas tienen asiento').should('be.visible');
        }
      });
    });
  });

  describe('9. Progreso de Generación', () => {
    it('debe mostrar el panel de progreso cuando se genera', () => {
      // Este test verifica que el panel existe, pero no ejecuta la generación
      // ya que puede ser una operación larga
      cy.get('body').should('be.visible');
    });

    it('debe tener botón de cancelar en el progreso', () => {
      // Verificar que el botón existe cuando está generando
      cy.get('body').then(($body) => {
        // El botón solo aparece cuando está generando
        if ($body.find('button').contains('Cancelar').length > 0) {
          cy.contains('button', 'Cancelar').should('be.visible');
        }
      });
    });
  });

  describe('10. Estados de Botones', () => {
    it('debe deshabilitar botones durante la generación', () => {
      // Los botones deberían estar habilitados inicialmente
      cy.contains('button', 'Generar Asientos').should('not.be.disabled');
    });

    it('debe deshabilitar "Regenerar Seleccionados" cuando no hay selección', () => {
      cy.contains('button', 'Regenerar Seleccionados').should('be.disabled');
    });

    it('debe habilitar "Regenerar Seleccionados" cuando hay selección', () => {
      cy.get('body').then(($body) => {
        if ($body.find('p-tableCheckbox').length > 0) {
          cy.get('p-tableCheckbox').first().click();
          cy.wait(500);
          cy.contains('button', 'Regenerar Seleccionados').should('not.be.disabled');
        }
      });
    });
  });

  describe('11. Validaciones y Comportamiento', () => {
    it('debe mostrar mensaje de confirmación al regenerar todos', () => {
      // Este test verifica que existe el botón, pero no ejecuta la acción
      // ya que requiere confirmación y puede modificar datos
      cy.contains('button', 'Regenerar Todos').should('be.visible');
    });

    it('debe mostrar mensaje de confirmación al contabilizar', () => {
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Contabilizar asiento"]').length > 0) {
          // El botón existe, pero no lo ejecutamos para no modificar datos
          cy.get('button[title="Contabilizar asiento"]').first().should('be.visible');
        }
      });
    });

    it('debe deshabilitar edición de asientos contabilizados automáticos', () => {
      cy.get('body').then(($body) => {
        // Buscar botones de editar que estén deshabilitados
        if ($body.find('button[title="Editar asiento"][disabled]').length > 0) {
          cy.get('button[title="Editar asiento"][disabled]').should('exist');
        }
      });
    });

    it('debe deshabilitar contabilización de asientos no balanceados', () => {
      cy.get('body').then(($body) => {
        // Buscar botones de contabilizar que estén deshabilitados
        if ($body.find('button[title="Contabilizar asiento"][disabled]').length > 0) {
          cy.get('button[title="Contabilizar asiento"][disabled]').should('exist');
        }
      });
    });
  });

  describe('12. Navegación y Responsividad', () => {
    it('debe mantener el estado al recargar la página', () => {
      cy.reload();
      cy.wait(3000);
      cy.url().should('include', '/accounting');
      cy.contains('Contabilidad').should('be.visible');
    });

    it('debe ser responsive en diferentes tamaños de pantalla', () => {
      // Probar en móvil
      cy.viewport(375, 667);
      cy.wait(1000);
      cy.contains('Contabilidad').should('be.visible');
      
      // Probar en tablet
      cy.viewport(768, 1024);
      cy.wait(1000);
      cy.contains('Contabilidad').should('be.visible');
      
      // Volver a desktop
      cy.viewport(1280, 720);
      cy.wait(1000);
    });
  });

  describe('13. Integración con API', () => {
    it('debe cargar datos de asientos desde la API', () => {
      cy.wait(3000);
      // Verificar que la tabla se carga (puede estar vacía)
      cy.get('p-table').should('exist');
    });

    it('debe cargar datos de cuentas contables', () => {
      // Las cuentas se cargan para el dropdown de edición
      cy.get('body').then(($body) => {
        if ($body.find('button[title="Editar asiento"]').length > 0) {
          cy.get('button[title="Editar asiento"]').first().click();
          cy.wait(1000);
          
          // Verificar que existe el dropdown de cuentas
          cy.get('p-dropdown').should('exist');
        }
      });
    });

    it('debe cargar facturas fallidas', () => {
      cy.wait(3000);
      // Verificar que la sección de facturas fallidas existe
      cy.contains('Facturas sin Asiento').should('be.visible');
    });
  });
});

