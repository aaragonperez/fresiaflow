import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { AppComponent } from './app.component';
import { routes } from './app.routes';
import { FRESIAFLOW_PROVIDERS } from '../infrastructure/api/providers';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    provideRouter(routes),
    provideAnimations(), // Necesario para PrimeNG
    ...FRESIAFLOW_PROVIDERS
  ]
});

