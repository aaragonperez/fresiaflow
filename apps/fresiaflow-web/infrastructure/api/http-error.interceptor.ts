import { Injectable, inject } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MessageService } from 'primeng/api';

/**
 * Interceptor HTTP que maneja errores de conexión y otros errores HTTP.
 * Muestra mensajes amigables al usuario cuando el backend no está disponible.
 */
@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  private messageService = inject(MessageService);
  private backendUnavailableShown = false;

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        // Detectar si el backend no está disponible
        if (this.isBackendUnavailable(error)) {
          // Solo mostrar el mensaje una vez para evitar spam
          if (!this.backendUnavailableShown) {
            this.messageService.add({
              severity: 'error',
              summary: 'Backend no disponible',
              detail: 'No se puede conectar con el servidor. Por favor, verifica que el backend esté ejecutándose en http://localhost:5000',
              life: 10000,
              closable: true
            });
            this.backendUnavailableShown = true;
            
            // Resetear el flag después de 5 segundos para permitir mostrar el mensaje nuevamente si es necesario
            setTimeout(() => {
              this.backendUnavailableShown = false;
            }, 5000);
          }
        } else if (error.status === 0) {
          // Error de conexión (CORS, red, etc.)
          this.messageService.add({
            severity: 'error',
            summary: 'Error de conexión',
            detail: 'No se pudo establecer conexión con el servidor. Verifica tu conexión a internet o que el backend esté ejecutándose.',
            life: 8000,
            closable: true
          });
        } else if (error.status >= 500) {
          // Errores del servidor
          this.messageService.add({
            severity: 'error',
            summary: 'Error del servidor',
            detail: error.error?.message || `Error interno del servidor (${error.status}). Por favor, inténtalo más tarde.`,
            life: 8000,
            closable: true
          });
        } else if (error.status === 404) {
          // Recurso no encontrado - solo loguear, no mostrar mensaje (puede ser normal)
          console.warn('Recurso no encontrado:', req.url);
        } else if (error.status === 401 || error.status === 403) {
          // No autorizado
          this.messageService.add({
            severity: 'warn',
            summary: 'No autorizado',
            detail: 'No tienes permisos para realizar esta acción.',
            life: 5000,
            closable: true
          });
        } else if (error.status >= 400 && error.status < 500) {
          // Errores del cliente - mostrar mensaje del backend si está disponible
          const errorMessage = error.error?.message || error.error?.title || error.message || 'Error en la solicitud';
          this.messageService.add({
            severity: 'warn',
            summary: 'Error en la solicitud',
            detail: errorMessage,
            life: 6000,
            closable: true
          });
        }

        return throwError(() => error);
      })
    );
  }

  /**
   * Detecta si el error indica que el backend no está disponible.
   */
  private isBackendUnavailable(error: HttpErrorResponse): boolean {
    // Error de conexión (status 0) o error de red
    if (error.status === 0) {
      return true;
    }

    // ECONNREFUSED, ETIMEDOUT, etc. en el mensaje
    const errorMessage = error.message?.toLowerCase() || '';
    if (errorMessage.includes('failed to fetch') || 
        errorMessage.includes('network error') ||
        errorMessage.includes('connection refused') ||
        errorMessage.includes('econnrefused')) {
      return true;
    }

    return false;
  }
}

