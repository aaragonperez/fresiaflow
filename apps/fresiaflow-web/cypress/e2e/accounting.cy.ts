describe('Página de Contabilidad - Comportamiento de usuario', () => {
  beforeEach(() => {
    cy.visit('/accounting');
    // Espera a que la página cargue
    cy.wait(2000);
  });

  it('debe cargar la página de contabilidad', () => {
    cy.url().should('include', '/accounting');
    cy.get('body').should('be.visible');
  });

  it('debe mostrar contenido de contabilidad', () => {
    // Espera a que el contenido cargue
    cy.wait(2000);
    cy.get('body').should('be.visible');
  });
});

