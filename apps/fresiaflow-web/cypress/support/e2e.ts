// ***********************************************************
// This example support/e2e.ts is processed and
// loaded automatically before your test files.
//
// This is a great place to put global configuration and
// behavior that modifies Cypress.
//
// You can change the location of this file or turn off
// automatically serving support files with the
// 'supportFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

// Manejar excepciones no capturadas de Angular
Cypress.on('uncaught:exception', (err, runnable) => {
  // Suprimir errores conocidos de Angular/Zone.js que no afectan las pruebas
  if (err.message.includes('NG0908') || 
      err.message.includes('Zone.js') ||
      err.message.includes('In this configuration Angular requires Zone.js')) {
    // Retornar false previene que Cypress falle la prueba
    return false;
  }
  // Para otros errores, permitir que Cypress los maneje normalmente
  return true;
});

// Suprimir warnings de tsconfig-paths y errores del renderer
if (typeof window !== 'undefined') {
  const originalError = window.console.error;
  const originalWarn = window.console.warn;
  
  window.console.error = function(...args: any[]) {
    // Filtrar errores del renderer (Electron y Chrome)
    const message = args.join(' ');
    if (message.includes('bad_message.cc') || 
        message.includes('Terminating renderer') ||
        message.includes('IPC message') ||
        message.includes('ERROR:bad_message')) {
      return; // Suprimir estos errores
    }
    originalError.apply(window.console, args);
  };
  
  window.console.warn = function(...args: any[]) {
    // Filtrar warnings relacionados
    const message = args.join(' ');
    if (message.includes('bad_message.cc') || 
        message.includes('Terminating renderer') ||
        message.includes('IPC message')) {
      return; // Suprimir estos warnings
    }
    originalWarn.apply(window.console, args);
  };
}

// Import commands.js using ES2015 syntax:
import './commands'

// Alternatively you can use CommonJS syntax:
// require('./commands')

