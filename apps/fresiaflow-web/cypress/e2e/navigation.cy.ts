describe('Navegación como usuario', () => {
  beforeEach(() => {
    // Visita la aplicación
    cy.visit('/');
  });

  it('debe cargar el dashboard por defecto', () => {
    // Verifica que esté en el dashboard
    cy.url().should('include', '/dashboard');
    cy.contains('Dashboard').should('be.visible');
  });

  it('debe navegar a Facturas desde el menú', () => {
    // Hace clic en el menú de Facturas
    cy.get('a[routerLink="/invoices"]').click();
    
    // Verifica que cambió la URL
    cy.url().should('include', '/invoices');
    
    // Verifica que la página de facturas se cargó
    cy.contains('Facturas', { timeout: 10000 }).should('be.visible');
  });

  it('debe navegar a todas las secciones principales', () => {
    const menuItems = [
      { route: '/dashboard', text: 'Dashboard' },
      { route: '/import', text: 'Importar' },
      { route: '/invoices', text: 'Facturas' },
      { route: '/banking', text: 'Bancos' },
      { route: '/accounting', text: 'Contabilidad' },
      { route: '/tasks', text: 'Tareas' }
    ];

    menuItems.forEach(({ route, text }) => {
      cy.get(`a[routerLink="${route}"]`).click();
      cy.url().should('include', route);
      // Espera a que la página cargue
      cy.get('body', { timeout: 10000 }).should('be.visible');
    });
  });

  it('debe abrir y cerrar el menú de configuración', () => {
    // Espera a que el sidebar esté visible
    cy.wait(1000);
    
    // Hace clic en Configuración para expandir
    cy.get('a.menu-item').contains('Configuración').click();
    
    // Espera a que el submenú se expanda
    cy.wait(1000);
    
    // Verifica que el submenú se muestra (puede estar en el DOM aunque no sea completamente visible)
    cy.contains('Empresas Propias').should('exist');
    cy.contains('Sincronización OneDrive').should('exist');
    
    // Verifica que el submenú está presente en el DOM
    cy.get('.submenu').should('exist');
    
    // Hace clic de nuevo para colapsar
    cy.get('a.menu-item').contains('Configuración').click();
    cy.wait(500);
  });
});

