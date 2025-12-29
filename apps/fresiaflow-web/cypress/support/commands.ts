/// <reference types="cypress" />

declare global {
  namespace Cypress {
    interface Chainable {
      /**
       * Navega a una página y espera a que cargue
       */
      visitPage(route: string): Chainable<void>;
      
      /**
       * Espera a que la tabla de PrimeNG cargue
       */
      waitForTable(): Chainable<void>;
    }
  }
}

Cypress.Commands.add('visitPage', (route: string) => {
  cy.visit(route);
  cy.wait(2000); // Espera a que la página cargue
});

Cypress.Commands.add('waitForTable', () => {
  cy.get('p-table', { timeout: 10000 }).should('be.visible');
});

export {};

