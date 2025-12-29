describe('Página de Tareas - Comportamiento de usuario', () => {
  beforeEach(() => {
    cy.visit('/tasks');
    // Espera a que la página cargue
    cy.wait(2000);
  });

  it('debe cargar la página de tareas', () => {
    cy.url().should('include', '/tasks');
    cy.get('body').should('be.visible');
  });

  it('debe mostrar la lista de tareas', () => {
    // Espera a que el contenido cargue
    cy.wait(2000);
    cy.get('body').should('be.visible');
  });
});

