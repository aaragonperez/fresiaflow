import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ChatMessage } from '../ui/components/fresia-chat/fresia-chat.component';

export interface ChatResponse {
  content: string;
  agent?: string;
}

@Injectable({ providedIn: 'root' })
export class FresiaChatService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/chat';
  
  // Historial conversacional en memoria (persistente durante la sesión)
  private messageHistory: Array<{ role: 'user' | 'assistant'; content: string }> = [];

  /**
   * Envía un mensaje al router FresiaFlow con histórico conversacional.
   */
  async sendMessage(message: string): Promise<ChatResponse> {
    // Agregar mensaje del usuario al histórico
    this.messageHistory.push({ role: 'user', content: message });

    try {
      // Enviar al backend con histórico
      const response = await firstValueFrom(
        this.http.post<ChatResponse>(this.baseUrl, {
          message: message,
          history: this.messageHistory
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

