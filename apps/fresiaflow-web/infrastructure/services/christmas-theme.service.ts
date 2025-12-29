import { Injectable, signal, computed, effect } from '@angular/core';

/**
 * Servicio para gestionar el tema navideño.
 * Se activa automáticamente entre el 24 de diciembre y el 6 de enero.
 */
@Injectable({ providedIn: 'root' })
export class ChristmasThemeService {
  private _enabled = signal(false);
  private _dismissed = signal(false);

  readonly enabled = this._enabled.asReadonly();
  readonly dismissed = this._dismissed.asReadonly();
  
  readonly isActive = computed(() => this._enabled() && !this._dismissed());

  constructor() {
    this.checkSeason();
    
    // Efecto para añadir/quitar la clase del body
    effect(() => {
      if (this.isActive()) {
        document.body.classList.add('christmas-theme');
      } else {
        document.body.classList.remove('christmas-theme');
      }
    });
  }

  /**
   * Verifica si estamos en temporada navideña (24 dic - 6 ene).
   */
  private checkSeason(): void {
    const now = new Date();
    const month = now.getMonth(); // 0-11
    const day = now.getDate();

    // Diciembre (11) del 24 al 31, o Enero (0) del 1 al 6
    const isChristmasSeason = 
      (month === 11 && day >= 24) || // 24-31 diciembre
      (month === 0 && day <= 6);      // 1-6 enero

    this._enabled.set(isChristmasSeason);

    // Verificar si el usuario lo descartó previamente en esta sesión
    const dismissedUntil = localStorage.getItem('christmas-dismissed-until');
    if (dismissedUntil) {
      const dismissedDate = new Date(dismissedUntil);
      if (dismissedDate > now) {
        this._dismissed.set(true);
      } else {
        localStorage.removeItem('christmas-dismissed-until');
      }
    }
  }

  /**
   * Descarta el tema navideño por 24 horas.
   */
  dismiss(): void {
    const tomorrow = new Date();
    tomorrow.setHours(tomorrow.getHours() + 24);
    localStorage.setItem('christmas-dismissed-until', tomorrow.toISOString());
    this._dismissed.set(true);
  }

  /**
   * Reactiva el tema navideño.
   */
  reactivate(): void {
    localStorage.removeItem('christmas-dismissed-until');
    this._dismissed.set(false);
  }
}

