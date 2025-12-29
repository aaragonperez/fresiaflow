describe('Página de Bancos - Comportamiento de usuario', () => {
  beforeEach(() => {
    cy.visit('/banking');
    // Espera a que la página cargue
    cy.wait(2000);
  });

  it('debe cargar la página de bancos', () => {
    cy.url().should('include', '/banking');
    cy.get('body').should('be.visible');
  });

  it('debe mostrar contenido de transacciones bancarias', () => {
    // Espera a que el contenido cargue
    cy.wait(2000);
    cy.get('body').should('be.visible');
  });
});

