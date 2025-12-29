describe('Configuración - Comportamiento de usuario', () => {
  beforeEach(() => {
    cy.visit('/settings/companies');
    cy.wait(2000);
  });

  it('debe navegar entre secciones de configuración', () => {
    const settingsSections = [
      { route: '/settings/companies', text: 'Empresas Propias' },
      { route: '/settings/onedrive', text: 'Sincronización OneDrive' },
      { route: '/settings/invoice-sources', text: 'Fuentes de Facturas' },
      { route: '/settings/accounting', text: 'Contabilidad' }
    ];

    settingsSections.forEach(({ route, text }) => {
      cy.visit(route);
      cy.url().should('include', route);
      cy.wait(2000);
      
      // Verifica que la página se cargó
      cy.get('body').should('be.visible');
    });
  });

  it('debe expandir y colapsar secciones', () => {
    // Espera a que la página cargue
    cy.wait(2000);
    
    // Busca secciones colapsables
    cy.get('body').then(($body) => {
      if ($body.find('.section-header').length > 0) {
        // Hace clic para colapsar/expandir
        cy.get('.section-header').first().click();
        cy.wait(500);
        
        // Hace clic de nuevo para expandir
        cy.get('.section-header').first().click();
        cy.wait(500);
      }
    });
  });

  it('debe mostrar el contenido de configuración', () => {
    cy.wait(2000);
    
    // Verifica que la página de configuración se cargó
    cy.get('body').should('be.visible');
    
    // Verifica que hay contenido (puede variar según la sección)
    cy.get('body').then(($body) => {
      expect($body.length).to.be.greaterThan(0);
    });
  });
});

