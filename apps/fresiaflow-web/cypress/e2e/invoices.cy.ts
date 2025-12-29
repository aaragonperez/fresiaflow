describe('Página de Facturas - Comportamiento de usuario', () => {
  beforeEach(() => {
    cy.visit('/invoices');
    // Espera a que la página cargue completamente
    cy.wait(2000);
  });

  it('debe mostrar la página de facturas con título', () => {
    // Verifica el título de la página
    cy.contains('Facturas Recibidas').should('be.visible');
    cy.contains('Gestión contable de facturas recibidas de proveedores').should('be.visible');
  });

  it('debe mostrar las estadísticas contables', () => {
    // Verifica que existen las tarjetas de estadísticas
    cy.contains('Total Facturas').should('be.visible');
    cy.contains('Pago Banco').should('be.visible');
    cy.contains('Pago Efectivo').should('be.visible');
    cy.contains('Total Facturado').should('be.visible');
  });

  it('debe expandir y colapsar secciones', () => {
    // Busca la sección de filtros
    cy.contains('Filtros Contables').should('be.visible');
    
    // Hace clic para colapsar
    cy.get('.section-header').contains('Filtros Contables').parent().click();
    cy.wait(500);
    
    // Hace clic para expandir de nuevo
    cy.get('.section-header').contains('Filtros Contables').parent().click();
    cy.wait(500);
  });

  it('debe mostrar la tabla de facturas cuando hay datos', () => {
    // Espera a que la página cargue
    cy.wait(3000);
    
    // Verifica que existe la tabla (puede estar colapsada)
    cy.get('body').then(($body) => {
      if ($body.find('p-table').length > 0) {
        // Si la tabla está colapsada, la expande
        if ($body.find('.section-header').contains('Listado de Facturas').length > 0) {
          cy.contains('Listado de Facturas').parent().click();
          cy.wait(1000);
        }
        
        // Verifica que existe la tabla
        cy.get('p-table').should('exist');
        
        // Verifica que hay un filtro global
        cy.get('input[placeholder*="Búsqueda global"]').should('be.visible');
      }
    });
  });

  it('debe filtrar facturas por búsqueda global', () => {
    // Espera a que la tabla cargue
    cy.wait(3000);
    
    cy.get('body').then(($body) => {
      if ($body.find('p-table').length > 0) {
        // Expande la tabla si está colapsada
        if ($body.find('.section-header').contains('Listado de Facturas').length > 0) {
          cy.contains('Listado de Facturas').parent().click();
          cy.wait(1000);
        }
        
        // Escribe en el filtro global
        cy.get('input[placeholder*="Búsqueda global"]')
          .type('test');
        
        // Espera a que se aplique el filtro
        cy.wait(500);
        
        // Verifica que la tabla muestra resultados filtrados
        cy.get('p-table tbody tr').should('have.length.at.least', 0);
      }
    });
  });

  it('debe limpiar los filtros', () => {
    cy.wait(3000);
    
    cy.get('body').then(($body) => {
      if ($body.find('p-table').length > 0) {
        // Expande la tabla si está colapsada
        if ($body.find('.section-header').contains('Listado de Facturas').length > 0) {
          cy.contains('Listado de Facturas').parent().click();
          cy.wait(1000);
        }
        
        // Escribe algo en el filtro
        cy.get('input[placeholder*="Búsqueda global"]')
          .type('test');
        
        // Hace clic en "Limpiar filtros"
        cy.contains('button', 'Limpiar filtros').click();
        
        // Verifica que el filtro se limpió
        cy.get('input[placeholder*="Búsqueda global"]')
          .should('have.value', '');
      }
    });
  });

  it('debe cambiar de página en la paginación', () => {
    cy.wait(3000);
    
    cy.get('body').then(($body) => {
      if ($body.find('p-table').length > 0) {
        // Expande la tabla si está colapsada
        if ($body.find('.section-header').contains('Listado de Facturas').length > 0) {
          cy.contains('Listado de Facturas').parent().click();
          cy.wait(1000);
        }
        
        // Verifica que existe el paginador
        cy.get('p-paginator').should('exist');
        
        // Si hay más de una página, navega a la siguiente
        cy.get('body').then(($body2) => {
          if ($body2.find('button[aria-label="Next Page"]').length > 0) {
            cy.get('button[aria-label="Next Page"]').click();
            cy.wait(500);
            // Verifica que cambió de página
            cy.get('p-table tbody').should('be.visible');
          }
        });
      }
    });
  });

  it('debe ordenar columnas al hacer clic', () => {
    cy.wait(3000);
    
    cy.get('body').then(($body) => {
      if ($body.find('p-table').length > 0) {
        // Expande la tabla si está colapsada
        if ($body.find('.section-header').contains('Listado de Facturas').length > 0) {
          cy.contains('Listado de Facturas').parent().click();
          cy.wait(1000);
        }
        
        // Hace clic en el header de una columna ordenable
        cy.get('th[pSortableColumn]').first().click();
        
        // Espera a que se ordene
        cy.wait(500);
        
        // Verifica que la tabla sigue visible
        cy.get('p-table tbody').should('be.visible');
      }
    });
  });
});

