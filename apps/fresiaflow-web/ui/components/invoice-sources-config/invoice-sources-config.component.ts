import { Component, OnInit, OnDestroy, inject, signal, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { ButtonModule } from 'primeng/button';
import { InputSwitchModule } from 'primeng/inputswitch';
import { DropdownModule } from 'primeng/dropdown';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ProgressBarModule } from 'primeng/progressbar';
import { MessageModule } from 'primeng/message';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import * as signalR from '@microsoft/signalr';
import { 
  InvoiceSourcesService, 
  InvoiceSource,
  CreateOrUpdateSourceRequest,
  SyncPreview,
  SyncResult,
  SourceValidationResult
} from '../../../infrastructure/services/invoice-sources.service';

@Component({
  selector: 'app-invoice-sources-config',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    InputTextareaModule,
    ButtonModule,
    InputSwitchModule,
    DropdownModule,
    ProgressSpinnerModule,
    ProgressBarModule,
    MessageModule,
    DialogModule,
    TableModule,
    TagModule
  ],
  templateUrl: './invoice-sources-config.component.html',
  styleUrl: './invoice-sources-config.component.css'
})
export class InvoiceSourcesConfigComponent implements OnInit, OnDestroy {
  private service = inject(InvoiceSourcesService);
  private ngZone = inject(NgZone);
  private hubConnection?: signalR.HubConnection;
  private refreshInterval?: number;

  // Expose service signals
  sources = this.service.sources;
  loading = this.service.loading;
  error = this.service.error;

  // Dialog state
  dialogVisible = signal(false);
  editingSource = signal<InvoiceSource | null>(null);
  
  // Form fields
  sourceType = signal<'Email' | 'Portal' | 'WebScraping' | 'OneDrive'>('Email');
  sourceName = signal('');
  configJson = signal('');
  enabled = signal(false);

  // Validation
  validationResult = signal<SourceValidationResult | null>(null);
  validating = signal(false);

  // Preview
  preview = signal<SyncPreview | null>(null);
  previewLoading = signal(false);

  // Sync
  syncing = signal<string | null>(null);
  
  // Sync progress (SignalR)
  syncProgress = signal(0);
  syncCurrentFile = signal('');
  syncProcessedCount = signal(0);
  syncTotalCount = signal(0);
  syncStatus = signal<'idle' | 'syncing' | 'completed' | 'error' | 'cancelled' | 'paused'>('idle');
  
  // Estado de sincronización pausada (para saber qué fuente está pausada)
  pausedSourceId = signal<string | null>(null);
  syncMessage = signal('');

  // Collapsible sections
  sourcesListExpanded = signal(true);
  newSourceExpanded = signal(false);

  sourceTypes = [
    { label: 'Email (IMAP/POP3)', value: 'Email' },
    { label: 'Portal Web', value: 'Portal' },
    { label: 'Web Scraping', value: 'WebScraping' },
    { label: 'OneDrive / SharePoint', value: 'OneDrive' }
  ];

  ngOnInit(): void {
    this.loadSources();
    this.initializeSignalR();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
    this.stopAutoRefresh();
  }

  private startAutoRefresh(): void {
    // Refrescar historial y estadísticas cada 60 segundos
    this.refreshInterval = window.setInterval(() => {
      this.ngZone.run(() => {
        this.loadSources();
      });
    }, 60000); // 60 segundos
  }

  private stopAutoRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = undefined;
    }
  }

  private initializeSignalR(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/sync-progress', {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling,
        withCredentials: false
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.elapsedMilliseconds < 60000) {
            return Math.random() * 5000;
          } else {
            return null;
          }
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

        this.hubConnection.on('ReceiveProgress', (update: any) => {
      console.log('✅ SignalR - Progreso recibido:', update);
      
      this.ngZone.run(() => {
        this.syncCurrentFile.set(update.currentFile || '');
        this.syncProcessedCount.set(update.processedCount || 0);
        this.syncTotalCount.set(update.totalCount || 0);
        this.syncProgress.set(update.percentage || 0);
        this.syncMessage.set(update.message || '');
        
        // Actualizar estado según el status del update
        if (update.status === 'paused') {
          this.syncStatus.set('paused');
          // Intentar detectar el sourceId desde el mensaje o buscar la fuente activa
          // Si hay una fuente sincronizando, usar su ID
          const activeSource = this.sources().find(s => this.syncing() === s.id);
          if (activeSource) {
            this.pausedSourceId.set(activeSource.id);
          }
        } else if (update.currentFile === 'Completado' || update.status === 'completed') {
          this.syncStatus.set('completed');
          setTimeout(() => {
            this.loadSources();
            this.syncStatus.set('idle');
          }, 2000);
        } else if (update.currentFile === 'Error' || update.status === 'error') {
          this.syncStatus.set('error');
        } else if (update.currentFile === 'Cancelado' || update.status === 'cancelled') {
          this.syncStatus.set('cancelled');
        } else if (update.currentFile || update.status === 'syncing') {
          this.syncStatus.set('syncing');
        }
      });
    });

    this.hubConnection.onclose((error) => {
      console.error('❌ SignalR - Conexión cerrada:', error);
    });

    this.hubConnection.onreconnecting((error) => {
      console.warn('⚠️ SignalR - Reconectando...', error);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('✅ SignalR - Reconectado:', connectionId);
    });

    this.hubConnection.start()
      .then(() => {
        console.log('✅ SignalR conectado correctamente. ConnectionId:', this.hubConnection?.connectionId);
      })
      .catch(err => {
        console.error('❌ Error conectando SignalR:', err);
      });
  }

  async loadSources(): Promise<void> {
    try {
      await this.service.loadAll();
    } catch (err) {
      console.error('Error cargando fuentes:', err);
    }
  }

  toggleSourcesList(): void {
    this.sourcesListExpanded.set(!this.sourcesListExpanded());
  }

  toggleNewSource(): void {
    this.newSourceExpanded.set(!this.newSourceExpanded());
    if (this.newSourceExpanded()) {
      this.resetForm();
    }
  }

  resetForm(): void {
    this.sourceType.set('Email');
    this.sourceName.set('');
    this.configJson.set('');
    this.enabled.set(false);
    this.editingSource.set(null);
    this.validationResult.set(null);
    this.preview.set(null);
  }

  openEditDialog(source: InvoiceSource): void {
    this.editingSource.set(source);
    this.sourceType.set(source.sourceType);
    this.sourceName.set(source.name);
    this.enabled.set(source.enabled);
    
    // Load full details
    this.service.getById(source.id).then(detail => {
      // Para OneDrive, asegurar que el JSON tenga syncIntervalMinutes preservado
      let configJson = detail.configJson;
      if (source.sourceType === 'OneDrive') {
        try {
          const jsonObj = JSON.parse(configJson);
          // Validar y preservar syncIntervalMinutes
          if (jsonObj.syncIntervalMinutes === undefined || 
              jsonObj.syncIntervalMinutes === null ||
              typeof jsonObj.syncIntervalMinutes !== 'number' ||
              jsonObj.syncIntervalMinutes < 1 || 
              jsonObj.syncIntervalMinutes > 1440) {
            // Si no está presente o es inválido, usar 15 como fallback
            // PERO solo si realmente no existe, no sobrescribir valores válidos
            if (jsonObj.syncIntervalMinutes === undefined || jsonObj.syncIntervalMinutes === null) {
              jsonObj.syncIntervalMinutes = 15;
              console.log('syncIntervalMinutes no encontrado, usando default: 15');
            } else {
              // Si existe pero es inválido, mantenerlo para que el usuario lo vea y lo corrija
              console.warn('syncIntervalMinutes inválido en BD:', jsonObj.syncIntervalMinutes);
            }
          } else {
            console.log('syncIntervalMinutes cargado correctamente:', jsonObj.syncIntervalMinutes);
          }
          configJson = JSON.stringify(jsonObj, null, 2);
        } catch (e) {
          console.warn('Error parseando JSON al cargar:', e);
        }
      }
      this.configJson.set(configJson);
      this.dialogVisible.set(true);
    }).catch(err => {
      console.error('Error cargando detalles:', err);
    });
  }

  closeDialog(): void {
    this.dialogVisible.set(false);
    this.editingSource.set(null);
    this.resetForm();
  }

  async validateConfig(): Promise<void> {
    if (!this.configJson().trim()) {
      this.validationResult.set({
        isValid: false,
        errorMessage: 'La configuración JSON no puede estar vacía'
      });
      return;
    }

    this.validating.set(true);
    this.validationResult.set(null);

    try {
      const result = await this.service.validate(this.sourceType(), this.configJson());
      this.validationResult.set(result);
    } catch (err: any) {
      this.validationResult.set({
        isValid: false,
        errorMessage: err.message || 'Error validando configuración'
      });
    } finally {
      this.validating.set(false);
    }
  }

  async getPreview(sourceId: string): Promise<void> {
    this.previewLoading.set(true);
    this.preview.set(null);

    try {
      const result = await this.service.getPreview(sourceId);
      this.preview.set(result);
    } catch (err) {
      console.error('Error obteniendo preview:', err);
    } finally {
      this.previewLoading.set(false);
    }
  }

  async saveSource(): Promise<void> {
    if (!this.sourceName().trim()) {
      alert('El nombre es obligatorio');
      return;
    }

    if (!this.configJson().trim()) {
      alert('La configuración JSON es obligatoria');
      return;
    }

    // Validate before saving
    await this.validateConfig();
    if (!this.validationResult()?.isValid) {
      alert('La configuración no es válida. Por favor, corrígela antes de guardar.');
      return;
    }

    // Para OneDrive, normalizar el JSON para asegurar que syncIntervalMinutes esté presente y sea válido
    let configJsonToSave = this.configJson();
    if (this.sourceType() === 'OneDrive' && this.editingSource()) {
      try {
        // Obtener el JSON existente del backend para preservar syncIntervalMinutes si no se proporciona uno nuevo
        const existingDetail = await this.service.getById(this.editingSource()!.id);
        const existingJson = JSON.parse(existingDetail.configJson);
        const existingInterval = (existingJson.syncIntervalMinutes && 
                                 typeof existingJson.syncIntervalMinutes === 'number' &&
                                 existingJson.syncIntervalMinutes >= 1 && 
                                 existingJson.syncIntervalMinutes <= 1440) 
                                 ? existingJson.syncIntervalMinutes 
                                 : 15;
        
        const jsonObj = JSON.parse(configJsonToSave);
        
        // Validar y normalizar syncIntervalMinutes
        if (jsonObj.syncIntervalMinutes !== undefined && jsonObj.syncIntervalMinutes !== null) {
          const interval = typeof jsonObj.syncIntervalMinutes === 'string' 
            ? parseInt(jsonObj.syncIntervalMinutes, 10) 
            : jsonObj.syncIntervalMinutes;
          
          // Validar rango (1-1440 minutos)
          if (isNaN(interval) || interval < 1 || interval > 1440) {
            alert(`syncIntervalMinutes debe estar entre 1 y 1440 minutos. Valor proporcionado: ${jsonObj.syncIntervalMinutes}`);
            return;
          }
          
          // Asegurar que sea un número
          jsonObj.syncIntervalMinutes = interval;
          console.log(`✅ Usando nuevo syncIntervalMinutes del usuario: ${interval}`);
        } else {
          // Si no está presente en el nuevo JSON, PRESERVAR el valor existente
          jsonObj.syncIntervalMinutes = existingInterval;
          console.log(`ℹ️ syncIntervalMinutes no proporcionado, preservando existente: ${existingInterval}`);
        }
        
        configJsonToSave = JSON.stringify(jsonObj);
        console.log('📤 JSON a guardar:', configJsonToSave);
      } catch (parseError) {
        console.error('❌ Error parseando JSON de OneDrive:', parseError);
        // Continuar con el JSON original si hay error de parseo
      }
    } else if (this.sourceType() === 'OneDrive' && !this.editingSource()) {
      // Nueva fuente: usar 15 por defecto
      try {
        const jsonObj = JSON.parse(configJsonToSave);
        if (jsonObj.syncIntervalMinutes === undefined || jsonObj.syncIntervalMinutes === null) {
          jsonObj.syncIntervalMinutes = 15;
          console.log('Nueva fuente OneDrive: usando syncIntervalMinutes default: 15');
        }
        configJsonToSave = JSON.stringify(jsonObj);
      } catch (parseError) {
        console.error('Error parseando JSON de OneDrive:', parseError);
      }
    }

    const request: CreateOrUpdateSourceRequest = {
      id: this.editingSource()?.id,
      sourceType: this.sourceType(),
      name: this.sourceName(),
      configJson: configJsonToSave,
      enabled: this.enabled()
    };

    try {
      await this.service.createOrUpdate(request);
      this.closeDialog();
      if (!this.editingSource()) {
        this.resetForm();
        this.newSourceExpanded.set(false);
      }
      await this.loadSources(); // Recargar para mostrar cambios
    } catch (err: any) {
      console.error('Error guardando fuente:', err);
      const errorMessage = err?.error?.error || err?.message || 'Error desconocido al guardar la fuente';
      alert(`Error al guardar la fuente: ${errorMessage}`);
    }
  }

  async deleteSource(source: InvoiceSource): Promise<void> {
    if (!confirm(`¿Estás seguro de que quieres eliminar la fuente "${source.name}"?`)) {
      return;
    }

    try {
      await this.service.delete(source.id);
    } catch (err) {
      console.error('Error eliminando fuente:', err);
    }
  }

  async resumePausedSync(sourceId: string): Promise<void> {
    try {
      await this.service.resumeSync(sourceId);
      this.syncStatus.set('syncing');
      this.syncMessage.set('Sincronización reanudada...');
      this.pausedSourceId.set(null);
    } catch (err: any) {
      console.error('Error reanudando sincronización:', err);
      alert('Error reanudando: ' + (err.error?.error || err.message || 'Error desconocido'));
    }
  }

  async syncSource(source: InvoiceSource): Promise<void> {
    this.syncing.set(source.id);
    this.syncStatus.set('syncing');
    this.syncProgress.set(0);
    this.syncMessage.set(`Iniciando sincronización de ${source.name}...`);
    this.pausedSourceId.set(null);
    try {
      const result = await this.service.sync(source.id, false);
      if (result.success) {
        this.syncMessage.set(`Completado: ${result.processedCount} procesadas, ${result.failedCount} fallidas, ${result.skippedCount} omitidas`);
        this.syncStatus.set('completed');
      } else {
        // Verificar si el error indica que está pausado
        if (result.errorMessage?.toLowerCase().includes('pausada') || 
            result.errorMessage?.toLowerCase().includes('pausado')) {
          this.syncStatus.set('paused');
          this.pausedSourceId.set(source.id);
        } else {
          this.syncMessage.set(`Error: ${result.errorMessage}`);
          this.syncStatus.set('error');
        }
      }
      await this.loadSources(); // Refresh
    } catch (err: any) {
      console.error('Error sincronizando:', err);
      this.syncMessage.set(`Error: ${err.message || 'Error desconocido'}`);
      this.syncStatus.set('error');
    } finally {
      this.syncing.set(null);
      setTimeout(() => {
        if (this.syncStatus() === 'completed' || this.syncStatus() === 'error') {
          this.syncStatus.set('idle');
          this.syncMessage.set('');
          this.pausedSourceId.set(null);
        }
      }, 5000);
    }
  }

  async syncAll(): Promise<void> {
    if (!confirm('¿Sincronizar todas las fuentes habilitadas?')) {
      return;
    }

    this.syncing.set('all');
    this.syncStatus.set('syncing');
    this.syncProgress.set(0);
    this.syncMessage.set('Iniciando sincronización de todas las fuentes...');
    try {
      const results = await this.service.syncAll(false);
      const total = Object.values(results).reduce((sum, r) => sum + r.processedCount, 0);
      this.syncMessage.set(`Completado: ${total} facturas procesadas en total`);
      this.syncStatus.set('completed');
      await this.loadSources(); // Refresh
    } catch (err: any) {
      console.error('Error sincronizando todas:', err);
      this.syncMessage.set(`Error: ${err.message || 'Error desconocido'}`);
      this.syncStatus.set('error');
    } finally {
      this.syncing.set(null);
      setTimeout(() => {
        if (this.syncStatus() === 'completed' || this.syncStatus() === 'error') {
          this.syncStatus.set('idle');
          this.syncMessage.set('');
        }
      }, 5000);
    }
  }

  getSourceTypeLabel(type: string | number): string {
    // Si es número, convertirlo a string primero
    let typeStr: string;
    if (typeof type === 'number') {
      const numberToTypeMap: Record<number, string> = {
        0: 'Email',
        1: 'Portal',
        2: 'WebScraping',
        3: 'OneDrive'
      };
      typeStr = numberToTypeMap[type] || 'Email';
    } else {
      typeStr = type;
    }
    
    if (typeStr === 'OneDrive') return 'OneDrive / SharePoint';
    return this.sourceTypes.find(t => t.value === typeStr)?.label || typeStr;
  }

  getSourceTypeIcon(type: string | number): string {
    // Si es número, convertirlo a string primero
    let typeStr: string;
    if (typeof type === 'number') {
      const numberToTypeMap: Record<number, string> = {
        0: 'Email',
        1: 'Portal',
        2: 'WebScraping',
        3: 'OneDrive'
      };
      typeStr = numberToTypeMap[type] || 'Email';
    } else {
      typeStr = type;
    }
    
    switch (typeStr) {
      case 'Email': return 'pi-envelope';
      case 'Portal': return 'pi-globe';
      case 'WebScraping': return 'pi-code';
      case 'OneDrive': return 'pi-cloud';
      default: return 'pi-file';
    }
  }

  formatDate(date: string | null): string {
    if (!date) return 'Nunca';
    return new Date(date).toLocaleString('es-ES');
  }

  getConfigPlaceholder(): string {
    const type = this.sourceType();
    switch (type) {
      case 'Email':
        return '{"useImap": true, "imapServer": "imap.gmail.com", "imapPort": 993, "username": "tu@email.com", "password": "tu_password", "useSsl": true, "filter": {"hasAttachment": true}}';
      case 'OneDrive':
        return '{"tenantId": "tu-tenant-id", "clientId": "tu-client-id", "clientSecret": "tu-client-secret", "folderPath": "/carpeta/facturas", "driveId": null, "syncIntervalMinutes": 15}';
      case 'Portal':
        return '{"baseUrl": "https://portal.ejemplo.com", "loginUrl": "https://portal.ejemplo.com/login", "username": "usuario", "password": "password", "invoiceLinkSelector": "a.invoice-link"}';
      case 'WebScraping':
        return '{"url": "https://ejemplo.com/facturas", "selectors": {"invoiceLinks": "a.invoice", "downloadButton": "button.download"}}';
      default:
        return 'Ingresa la configuración JSON según el tipo de fuente seleccionado.';
    }
  }
}

