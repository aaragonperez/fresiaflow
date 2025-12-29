import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { AppComponent } from './app.component';
import { routes } from './app.routes';
import { FRESIAFLOW_PROVIDERS } from '../infrastructure/api/providers';
import { HttpErrorInterceptor } from '../infrastructure/api/http-error.interceptor';
import { MessageService } from 'primeng/api';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    provideRouter(routes),
    provideAnimations(), // Necesario para PrimeNG
    MessageService, // Servicio global para mostrar mensajes
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HttpErrorInterceptor,
      multi: true
    },
    ...FRESIAFLOW_PROVIDERS
  ]
});

