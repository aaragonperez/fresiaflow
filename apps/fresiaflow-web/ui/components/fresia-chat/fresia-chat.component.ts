import { Component, OnInit, inject, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FresiaChatService } from '../../../application/fresia-chat.service';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  agent?: string;
  timestamp: Date;
}

@Component({
  selector: 'app-fresia-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './fresia-chat.component.html',
  styleUrls: ['./fresia-chat.component.css']
})
export class FresiaChatComponent implements OnInit, AfterViewChecked {
  private chatService = inject(FresiaChatService);
  
  isOpen = signal<boolean>(false);
  messages = signal<ChatMessage[]>([]);
  currentMessage = signal<string>('');
  isLoading = signal<boolean>(false);
  
  @ViewChild('messagesContainer') messagesContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('messageInput') messageInput!: ElementRef<HTMLInputElement>;
  
  private shouldScroll = false;

  ngOnInit() {
    // Cargar mensajes del servicio
    this.messages.set(this.chatService.getHistory());
  }

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  toggleChat() {
    this.isOpen.set(!this.isOpen());
    if (this.isOpen()) {
      // Focus en el input cuando se abre
      setTimeout(() => {
        this.messageInput?.nativeElement?.focus();
      }, 100);
    }
  }

  async sendMessage() {
    const message = this.currentMessage().trim();
    if (!message || this.isLoading()) return;

    // Agregar mensaje del usuario
    const userMessage: ChatMessage = {
      role: 'user',
      content: message,
      timestamp: new Date()
    };
    this.messages.update(msgs => [...msgs, userMessage]);
    this.currentMessage.set('');
    this.shouldScroll = true;
    this.isLoading.set(true);

    try {
      // Enviar al servicio que usa el router FresiaFlow
      const response = await this.chatService.sendMessage(message);
      
      // Agregar respuesta del asistente
      const assistantMessage: ChatMessage = {
        role: 'assistant',
        content: response.content,
        agent: response.agent,
        timestamp: new Date()
      };
      this.messages.update(msgs => [...msgs, assistantMessage]);
      this.shouldScroll = true;
    } catch (error) {
      console.error('Error en el chat:', error);
      const errorMessage: ChatMessage = {
        role: 'assistant',
        content: 'Error al procesar tu mensaje. Por favor, inténtalo de nuevo.',
        timestamp: new Date()
      };
      this.messages.update(msgs => [...msgs, errorMessage]);
      this.shouldScroll = true;
    } finally {
      this.isLoading.set(false);
    }
  }

  onKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  clearHistory() {
    this.chatService.clearHistory();
    this.messages.set([]);
  }

  /**
   * Pregunta una sugerencia predefinida.
   */
  askSuggestion(question: string) {
    this.currentMessage.set(question);
    this.sendMessage();
  }

  private scrollToBottom() {
    if (this.messagesContainer) {
      const element = this.messagesContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }
}

