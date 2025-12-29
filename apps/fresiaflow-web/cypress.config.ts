import { defineConfig } from 'cypress';

// Suprimir warnings de tsconfig-paths antes de cargar la configuración
if (typeof process !== 'undefined') {
  const originalWarn = console.warn;
  const originalError = console.error;
  
  console.warn = function(...args: any[]) {
    const message = args[0];
    if (typeof message === 'string' && message.includes('Missing baseUrl')) {
      return; // Suprimir este warning específico
    }
    originalWarn.apply(console, args);
  };
  
  // Suprimir errores del renderer en la consola de Node
  console.error = function(...args: any[]) {
    const message = args.join(' ');
    if (message.includes('bad_message.cc') || 
        message.includes('Terminating renderer') ||
        message.includes('IPC message')) {
      return; // Suprimir estos errores
    }
    originalError.apply(console, args);
  };
}

export default defineConfig({
  projectId: '9dy2ix',
  e2e: {
    baseUrl: 'http://localhost:4200',
    setupNodeEvents(on, config) {
      // Filtrar errores de stderr del renderer
      const originalStderrWrite = process.stderr.write.bind(process.stderr);
      process.stderr.write = function(chunk: any, encoding?: any, fd?: any) {
        const message = chunk?.toString() || '';
        if (message.includes('bad_message.cc') || 
            message.includes('Terminating renderer') ||
            message.includes('IPC message') ||
            message.includes('ERROR:bad_message')) {
          // Suprimir estos mensajes
          return true;
        }
        return originalStderrWrite(chunk, encoding, fd);
      };
      
      // Manejar errores del renderer tanto en Electron como en Chrome
      on('before:browser:launch', (browser, launchOptions) => {
        // Aplicar a todos los navegadores (Chrome y Electron)
        launchOptions.args = launchOptions.args || [];
        
        // Argumentos para suprimir errores del renderer
        launchOptions.args.push('--disable-logging');
        launchOptions.args.push('--log-level=3');
        launchOptions.args.push('--disable-dev-shm-usage');
        launchOptions.args.push('--disable-ipc-flooding-protection');
        launchOptions.args.push('--disable-background-networking');
        launchOptions.args.push('--disable-background-timer-throttling');
        launchOptions.args.push('--disable-renderer-backgrounding');
        launchOptions.args.push('--disable-features=TranslateUI');
        launchOptions.args.push('--disable-features=VizDisplayCompositor,NetworkService,NetworkServiceInProcess');
        launchOptions.args.push('--disable-breakpad');
        launchOptions.args.push('--no-sandbox');
        launchOptions.args.push('--disable-setuid-sandbox');
        launchOptions.args.push('--disable-web-security');
        launchOptions.args.push('--disable-features=IsolateOrigins,site-per-process');
        launchOptions.args.push('--silent');
        launchOptions.args.push('--disable-gpu-logging');
        
        // Para Electron específicamente
        if (browser.name === 'electron') {
          launchOptions.preferences = {
            ...launchOptions.preferences,
            'disable-features': 'VizDisplayCompositor',
            'enable-logging': false,
          };
        }
        
        return launchOptions;
      });
    },
    viewportWidth: 1280,
    viewportHeight: 720,
    video: true,
    screenshotOnRunFailure: true,
    defaultCommandTimeout: 10000,
    requestTimeout: 10000,
    responseTimeout: 10000,
    // Configuración para evitar problemas
    chromeWebSecurity: false,
    experimentalStudio: true,
  },
});

