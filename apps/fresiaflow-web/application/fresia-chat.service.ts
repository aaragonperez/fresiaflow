import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ChatMessage } from '../ui/components/fresia-chat/fresia-chat.component';

export interface ChatAction {
  type: string;
  params: Record<string, any>;
}

export interface ChatResponse {
  content: string;
  agent?: string;
  actions?: ChatAction[];
}

export interface ScreenContext {
  route: string;
  pageTitle: string;
  visibleData?: any; // Datos visibles en la pantalla (tablas, formularios, etc.)
  availableActions?: string[]; // Acciones disponibles en la pantalla
  componentState?: Record<string, any>; // Estado de los componentes
}

@Injectable({ providedIn: 'root' })
export class FresiaChatService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/chat';
  
  // Historial conversacional en memoria (persistente durante la sesión)
  private messageHistory: Array<{ role: 'user' | 'assistant'; content: string }> = [];

  /**
   * Envía un mensaje al chat con contexto de la pantalla actual.
   */
  async sendMessage(message: string, screenContext?: ScreenContext): Promise<ChatResponse> {
    // Agregar mensaje del usuario al histórico
    this.messageHistory.push({ role: 'user', content: message });

    try {
      // Enviar al endpoint de chat con contexto de pantalla
      const response = await firstValueFrom(
        this.http.post<ChatResponse>(this.baseUrl, {
          message: message,
          history: this.messageHistory.slice(0, -1), // Excluir el mensaje actual que acabamos de agregar
          screenContext: screenContext
        })
      );

      // Agregar respuesta del asistente al histórico
      this.messageHistory.push({ 
        role: 'assistant', 
        content: response.content 
      });

      return response;
    } catch (error) {
      // Revertir el mensaje del usuario si hay error
      this.messageHistory.pop();
      throw error;
    }
  }

  /**
   * Obtiene el historial de mensajes.
   */
  getHistory(): ChatMessage[] {
    return this.messageHistory.map((msg, index) => ({
      role: msg.role,
      content: msg.content,
      timestamp: new Date() // En producción, guardar timestamps reales
    }));
  }

  /**
   * Limpia el historial conversacional.
   */
  clearHistory(): void {
    this.messageHistory = [];
  }

  /**
   * Obtiene el historial en formato para el backend.
   */
  getHistoryForBackend(): Array<{ role: 'user' | 'assistant'; content: string }> {
    return [...this.messageHistory];
  }
}

