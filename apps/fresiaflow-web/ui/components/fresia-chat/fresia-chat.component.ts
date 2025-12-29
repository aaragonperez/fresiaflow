import { Component, OnInit, inject, signal, ViewChild, ElementRef, AfterViewChecked, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { FresiaChatService, ScreenContext } from '../../../application/fresia-chat.service';
import { ChatActionService } from '../../../application/chat-action.service';

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
export class FresiaChatComponent implements OnInit, AfterViewChecked, OnDestroy {
  private chatService = inject(FresiaChatService);
  private actionService = inject(ChatActionService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private routerSubscription?: Subscription;
  
  isOpen = signal<boolean>(false);
  messages = signal<ChatMessage[]>([]);
  currentMessage = signal<string>('');
  isLoading = signal<boolean>(false);
  chatTitle = signal<string>('Asistente IA');
  
  @ViewChild('messagesContainer') messagesContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('messageInput') messageInput!: ElementRef<HTMLInputElement>;
  
  private shouldScroll = false;

  ngOnInit() {
    // Cargar mensajes del servicio
    this.messages.set(this.chatService.getHistory());
    
    // Actualizar título según la ruta inicial
    this.updateChatTitle(this.router.url);
    
    // Suscribirse a cambios de ruta
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.updateChatTitle(event.urlAfterRedirects || event.url);
      });
  }

  ngOnDestroy() {
    this.routerSubscription?.unsubscribe();
  }

  private updateChatTitle(url: string): void {
    if (url.includes('/invoices')) {
      this.chatTitle.set('Asistente de Facturas');
    } else if (url.includes('/import')) {
      this.chatTitle.set('Asistente de Importación');
    } else if (url.includes('/settings/companies')) {
      this.chatTitle.set('Asistente de Empresas');
    } else if (url.includes('/settings/onedrive')) {
      this.chatTitle.set('Asistente de OneDrive');
    } else if (url.includes('/settings')) {
      this.chatTitle.set('Asistente de Configuración');
    } else if (url.includes('/dashboard')) {
      this.chatTitle.set('Asistente de Dashboard');
    } else if (url.includes('/help')) {
      this.chatTitle.set('Asistente de Ayuda');
    } else {
      this.chatTitle.set('Asistente IA');
    }
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
      // Capturar contexto de la pantalla actual
      const screenContext = this.captureScreenContext();
      
      // Enviar al servicio con contexto de pantalla
      const response = await this.chatService.sendMessage(message, screenContext);
      
      // Agregar respuesta del asistente
      const assistantMessage: ChatMessage = {
        role: 'assistant',
        content: response.content,
        agent: response.agent,
        timestamp: new Date()
      };
      this.messages.update(msgs => [...msgs, assistantMessage]);
      this.shouldScroll = true;

      // Ejecutar acciones si existen
      if (response.actions && response.actions.length > 0) {
        response.actions.forEach(action => {
          this.actionService.executeAction(action);
        });
      }
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

  /**
   * Captura el contexto de la pantalla actual para enviarlo al chat.
   */
  private captureScreenContext(): ScreenContext {
    const context: ScreenContext = {
      route: this.router.url,
      pageTitle: this.chatTitle(),
      availableActions: [],
      componentState: {}
    };

    // Capturar datos visibles según la ruta
    try {
      const mainContent = document.querySelector('main, .main-content, [role="main"]');
      if (mainContent) {
        // Intentar capturar datos de tablas
        const tables = mainContent.querySelectorAll('table, p-table');
        if (tables.length > 0) {
          context.visibleData = {
            tables: Array.from(tables).map((table, index) => {
              const rows = table.querySelectorAll('tbody tr, .p-datatable-tbody tr');
              return {
                index,
                rowCount: rows.length,
                headers: Array.from(table.querySelectorAll('thead th, .p-datatable-thead th')).map(th => th.textContent?.trim()).filter(Boolean)
              };
            })
          };
        }

        // Capturar formularios visibles
        const forms = mainContent.querySelectorAll('form, .form-group');
        if (forms.length > 0) {
          context.componentState = {
            forms: Array.from(forms).map((form, index) => {
              const inputs = form.querySelectorAll('input, select, textarea');
              return {
                index,
                inputCount: inputs.length,
                inputTypes: Array.from(inputs).map(input => (input as HTMLElement).tagName.toLowerCase())
              };
            })
          };
        }

        // Capturar botones disponibles
        const buttons = mainContent.querySelectorAll('button, [role="button"]');
        context.availableActions = Array.from(buttons)
          .map(btn => {
            const text = btn.textContent?.trim() || (btn as HTMLElement).getAttribute('title') || (btn as HTMLElement).getAttribute('aria-label');
            return text;
          })
          .filter(Boolean) as string[];
      }
    } catch (error) {
      console.warn('Error capturando contexto de pantalla:', error);
    }

    return context;
  }
}

