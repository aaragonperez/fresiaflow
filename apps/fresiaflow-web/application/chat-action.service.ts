import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { ChatAction } from './fresia-chat.service';

/**
 * Servicio para comunicar acciones del chat a los componentes de la aplicación.
 * Permite que el chat ejecute acciones en las páginas (filtros, navegación, etc.)
 */
@Injectable({ providedIn: 'root' })
export class ChatActionService {
  private actionSubject = new Subject<ChatAction>();

  /**
   * Observable para escuchar acciones del chat.
   */
  action$ = this.actionSubject.asObservable();

  /**
   * Ejecuta una acción recibida del chat.
   */
  executeAction(action: ChatAction): void {
    this.actionSubject.next(action);
  }
}

