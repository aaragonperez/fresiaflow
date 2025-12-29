import { Component, OnInit, inject, signal, computed, ViewChild, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputSwitchModule } from 'primeng/inputswitch';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressBarModule } from 'primeng/progressbar';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import * as signalR from '@microsoft/signalr';
import { 
  OneDriveSyncService, 
  SyncedFile,
  SyncPreview
} from '../../../infrastructure/services/onedrive-sync.service';
import {
  InvoiceSourcesService,
  InvoiceSource,
  SyncResult as SourceSyncResult
} from '../../../infrastructure/services/invoice-sources.service';

@Component({
  selector: 'app-import-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputSwitchModule,
    TableModule,
    TagModule,
    TooltipModule,
    ProgressBarModule,
    ProgressSpinnerModule,
    MessageModule,
    CardModule,
    DividerModule,
    DialogModule,
    InputTextModule
  ],
  templateUrl: './import-page.component.html',
  styleUrl: './import-page.component.css'
})
export class ImportPageComponent implements OnInit, OnDestroy {
  private oneDriveSyncService = inject(OneDriveSyncService);
  private invoiceSourcesService = inject(InvoiceSourcesService);
  private http = inject(HttpClient);
  private ngZone = inject(NgZone);
  private hubConnection?: signalR.HubConnection;
  private syncCancelled = false;

  @ViewChild('dt') table!: Table;

  // Sync options
  forceReprocess = signal(false);
  syncResult = signal<{ success: boolean; message: string; detailedErrors?: string[] } | null>(null);

  // Error dialog
  errorDialogVisible = signal(false);
  selectedFileForError = signal<SyncedFile | null>(null);

  // Fuentes configuradas
  sources = this.invoiceSourcesService.sources;
  sourcesLoading = this.invoiceSourcesService.loading;
  
  // Estado de sincronización por fuente
  syncingSourceId = signal<string | null>(null);
  syncResults = signal<Record<string, SourceSyncResult>>({});
  syncInitializing = signal(false); // Indica que se está iniciando la sincronización
  
  // Estado detallado de sincronización desde SignalR
  currentSyncSourceId = signal<string | null>(null);
  currentSyncSourceName = signal<string | null>(null);
  currentSyncSourceType = signal<string | null>(null);
  currentSyncStage = signal<string | null>(null);
  syncProcessedFiles = signal(0);
  syncFailedFiles = signal(0);
  syncSkippedFiles = signal(0);
  syncAlreadyExistedFiles = signal(0);
  currentFileStatus = signal<string | null>(null);
  currentFileSize = signal<number | null>(null);
  currentFileError = signal<string | null>(null);
  
  // Expose OneDrive service signals (para compatibilidad y historial)
  config = this.oneDriveSyncService.config;
  syncedFiles = this.oneDriveSyncService.syncedFiles;
  loading = this.oneDriveSyncService.loading;
  syncing = this.oneDriveSyncService.syncing;
  error = this.oneDriveSyncService.error;
  syncProgress = this.oneDriveSyncService.syncProgress;
  syncCurrentFile = this.oneDriveSyncService.syncCurrentFile;
  syncProcessedCount = this.oneDriveSyncService.syncProcessedCount;
  syncTotalCount = this.oneDriveSyncService.syncTotalCount;
  syncStatus = this.oneDriveSyncService.syncStatus;
  syncMessage = this.oneDriveSyncService.syncMessage;

  // Computed statistics globales
  statsCompleted = computed(() => this.syncedFiles().filter(f => f.status === 'Completed').length);
  statsFailed = computed(() => this.syncedFiles().filter(f => f.status === 'Failed').length);
  statsSkipped = computed(() => this.syncedFiles().filter(f => f.status === 'Skipped').length);
  statsWithInvoice = computed(() => this.syncedFiles().filter(f => !!f.invoiceId).length);
  statsWithoutInvoice = computed(() => this.syncedFiles().filter(f => !f.invoiceId).length);
  
  // Estadísticas por fuente
  getSourceStats(sourceId: string) {
    const sourcePrefix = this.getSourcePrefix(sourceId);
    const files = this.syncedFiles().filter(f => f.source?.startsWith(sourcePrefix));
    return {
      total: files.length,
      completed: files.filter(f => f.status === 'Completed').length,
      failed: files.filter(f => f.status === 'Failed').length,
      skipped: files.filter(f => f.status === 'Skipped').length,
      withInvoice: files.filter(f => !!f.invoiceId).length
    };
  }
  
  private getSourcePrefix(sourceId: string): string {
    const source = this.sources().find(s => s.id === sourceId);
    if (!source) return '';
    return `${source.sourceType}-${sourceId}`;
  }

  // Preview de sincronización
  syncPreview = signal<SyncPreview | null>(null);
  loadingPreview = signal(false);

  // Secciones colapsables
  syncSectionExpanded = signal(true);
  previewSectionExpanded = signal(true);
  statsSectionExpanded = signal(true);
  historySectionExpanded = signal(true);

  // Toggle secciones
  toggleSyncSection(): void { this.syncSectionExpanded.set(!this.syncSectionExpanded()); }
  togglePreviewSection(): void { this.previewSectionExpanded.set(!this.previewSectionExpanded()); }
  toggleStatsSection(): void { this.statsSectionExpanded.set(!this.statsSectionExpanded()); }
  toggleHistorySection(): void { this.historySectionExpanded.set(!this.historySectionExpanded()); }

  ngOnInit(): void {
    this.loadData();
    this.initializeSignalR();
    this.checkSyncStatus();
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop();
  }

  private async loadData(): Promise<void> {
    // Cargar todas las fuentes
    await this.invoiceSourcesService.loadAll();
    
    // Cargar datos de OneDrive (para compatibilidad y historial)
    await this.oneDriveSyncService.loadConfig();
    await this.oneDriveSyncService.loadSyncedFiles();
    await this.loadSyncPreview();
  }

  private async checkSyncStatus(): Promise<void> {
    // Verificar si hay una sincronización en progreso
    try {
      const status = await this.oneDriveSyncService.getSyncStatus();
      if (status.isSyncing) {
        // Hay una sincronización en progreso, mostrar estado
        this.oneDriveSyncService.syncStatus.set('syncing');
        this.syncInitializing.set(false);
        this.oneDriveSyncService.syncMessage.set('Sincronización en progreso... Esperando actualizaciones...');
        
        // Intentar identificar qué fuente está sincronizando
        // Si es OneDrive, podemos obtener más información
        const sources = this.sources();
        const oneDriveSource = sources.find(s => s.sourceType === 'OneDrive');
        if (oneDriveSource) {
          this.syncingSourceId.set(oneDriveSource.id);
          this.currentSyncSourceName.set(oneDriveSource.name);
          this.currentSyncSourceType.set('OneDrive');
        }
        
        // SignalR se encargará de actualizar el progreso cuando se reconecte
        // Mientras tanto, mostrar el estado de "en progreso"
      } else {
        // No hay sincronización en progreso, asegurar que el estado esté limpio
        if (this.oneDriveSyncService.syncStatus() === 'syncing') {
          this.oneDriveSyncService.syncStatus.set('idle');
          this.syncingSourceId.set(null);
          this.syncInitializing.set(false);
        }
      }
    } catch (err) {
      console.error('Error verificando estado de sincronización:', err);
    }
  }

  async loadSyncPreview(): Promise<void> {
    // Solo cargar preview de OneDrive si está configurado (para mantener compatibilidad)
    if (!this.config()?.configured) return;
    
    this.loadingPreview.set(true);
    try {
      const preview = await this.oneDriveSyncService.getSyncPreview();
      this.syncPreview.set(preview);
    } catch (err) {
      console.error('Error cargando preview:', err);
    } finally {
      this.loadingPreview.set(false);
    }
  }
  
  async syncSource(source: InvoiceSource): Promise<void> {
    if (this.syncingSourceId() === source.id) {
      console.warn('Ya hay una sincronización en progreso para esta fuente');
      return;
    }
    
    // Inicializar estado de sincronización inmediatamente
    this.syncingSourceId.set(source.id);
    this.syncInitializing.set(true);
    this.syncResult.set(null);
    this.oneDriveSyncService.syncStatus.set('syncing');
    this.oneDriveSyncService.syncMessage.set(`Iniciando sincronización de ${source.name}...`);
    this.currentSyncSourceName.set(source.name);
    this.currentSyncSourceType.set(source.sourceType);
    this.currentSyncStage.set('initializing');
    
    // Limpiar resultado previo de esta fuente
    const currentResults = { ...this.syncResults() };
    delete currentResults[source.id];
    this.syncResults.set(currentResults);
    
    try {
      const result = await this.invoiceSourcesService.sync(source.id, this.forceReprocess());
      
      // Actualizar resultados
      this.syncResults.set({
        ...this.syncResults(),
        [source.id]: result
      });
      
      // Recargar datos para actualizar estadísticas y historial
      await Promise.all([
        this.oneDriveSyncService.loadSyncedFiles(),
        this.invoiceSourcesService.loadAll()
      ]);
      
      const message = result.success
        ? `Sincronización completada: ${result.processedCount} procesados, ${result.failedCount} fallidos, ${result.skippedCount} omitidos`
        : `Error: ${result.errorMessage || 'Error desconocido'}`;
      
      this.syncResult.set({
        success: result.success,
        message,
        detailedErrors: result.detailedErrors
      });
    } catch (err: any) {
      const errorMessage = err.error?.error || err.message || 'Error durante la sincronización';
      this.syncResult.set({
        success: false,
        message: errorMessage
      });
      // También actualizar resultados con el error
      this.syncResults.set({
        ...this.syncResults(),
        [source.id]: {
          success: false,
          processedCount: 0,
          failedCount: 0,
          skippedCount: 0,
          errorMessage,
          detailedErrors: [errorMessage]
        }
      });
    } finally {
      this.syncInitializing.set(false);
      // No limpiar syncingSourceId aquí, SignalR lo hará cuando termine
      // Solo limpiar si no hay actualizaciones de SignalR en los próximos 5 segundos
      setTimeout(() => {
        if (this.syncingSourceId() === source.id) {
          this.syncingSourceId.set(null);
          this.oneDriveSyncService.syncStatus.set('idle');
        }
      }, 5000);
    }
  }
  
  // Métodos unificados que funcionan tanto con sourceType directo como con source string
  getSourceTypeIcon(sourceTypeOrSource?: string): string {
    if (!sourceTypeOrSource) return 'pi-file';
    
    // Si es un source string (ej: "OneDrive-{id}"), extraer el tipo
    const sourceType = sourceTypeOrSource.includes('-') 
      ? this.extractSourceType(sourceTypeOrSource)
      : sourceTypeOrSource;
    
    switch (sourceType) {
      case 'Email': return 'pi-envelope';
      case 'Portal': return 'pi-globe';
      case 'WebScraping': return 'pi-code';
      case 'OneDrive': return 'pi-cloud';
      default: return 'pi-file';
    }
  }
  
  getSourceTypeLabel(sourceTypeOrSource?: string): string {
    if (!sourceTypeOrSource) return 'Desconocido';
    
    // Si es un source string (ej: "OneDrive-{id}"), extraer el tipo
    const sourceType = sourceTypeOrSource.includes('-') 
      ? this.extractSourceType(sourceTypeOrSource)
      : sourceTypeOrSource;
    
    switch (sourceType) {
      case 'Email': return 'Email (IMAP/POP3)';
      case 'Portal': return 'Portal Web';
      case 'WebScraping': return 'Web Scraping';
      case 'OneDrive': return 'OneDrive / SharePoint';
      default: return sourceType || 'Desconocido';
    }
  }

  private initializeSignalR(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/sync-progress', {
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
      
      if (this.syncCancelled) {
        console.log('⚠️ Sincronización cancelada, ignorando actualizaciones');
        return;
      }

      this.ngZone.run(() => {
        // Cuando llega la primera actualización de SignalR, ya no estamos inicializando
        this.syncInitializing.set(false);
        
        // Actualizar información básica (compatibilidad con código existente)
        this.oneDriveSyncService.syncCurrentFile.set(update.currentFile || update.currentFileName || '');
        this.oneDriveSyncService.syncProcessedCount.set(update.processedCount || 0);
        this.oneDriveSyncService.syncTotalCount.set(update.totalCount || 0);
        this.oneDriveSyncService.syncProgress.set(update.percentage || 0);
        this.oneDriveSyncService.syncMessage.set(update.message || '');
        
        // Actualizar información detallada
        if (update.sourceId) {
          this.currentSyncSourceId.set(update.sourceId);
        }
        if (update.sourceName) {
          this.currentSyncSourceName.set(update.sourceName);
        }
        if (update.sourceType) {
          this.currentSyncSourceType.set(update.sourceType);
        }
        if (update.stage) {
          this.currentSyncStage.set(update.stage);
        }
        if (update.processedFiles !== undefined) {
          this.syncProcessedFiles.set(update.processedFiles);
        }
        if (update.failedFiles !== undefined) {
          this.syncFailedFiles.set(update.failedFiles);
        }
        if (update.skippedFiles !== undefined) {
          this.syncSkippedFiles.set(update.skippedFiles);
        }
        if (update.alreadyExistedFiles !== undefined) {
          this.syncAlreadyExistedFiles.set(update.alreadyExistedFiles);
        }
        if (update.currentFileStatus) {
          this.currentFileStatus.set(update.currentFileStatus);
        }
        if (update.currentFileSize !== undefined) {
          this.currentFileSize.set(update.currentFileSize);
        }
        if (update.currentFileError) {
          this.currentFileError.set(update.currentFileError);
        } else {
          this.currentFileError.set(null);
        }
        
        // Determinar estado general
        if (update.currentFile === 'Completado' || update.status === 'completed') {
          this.oneDriveSyncService.syncStatus.set('completed');
          this.syncInitializing.set(false); // Limpiar estado de inicialización
          // Limpiar estado de sincronización
          this.syncingSourceId.set(null);
          // Recargar datos inmediatamente
          this.oneDriveSyncService.loadSyncedFiles();
          this.oneDriveSyncService.loadConfig();
          this.invoiceSourcesService.loadAll();
          // Limpiar estado detallado después de un breve delay
          setTimeout(() => {
            this.currentSyncSourceId.set(null);
            this.currentSyncSourceName.set(null);
            this.currentSyncSourceType.set(null);
            this.currentSyncStage.set(null);
            this.currentFileStatus.set(null);
            this.currentFileError.set(null);
            this.currentFileSize.set(null);
            // Limpiar resultados de sincronización después de mostrar el mensaje
            setTimeout(() => {
              this.syncResults.set({});
            }, 5000);
          }, 2000);
        } else if (update.currentFile === 'Error' || update.status === 'error') {
          this.oneDriveSyncService.syncStatus.set('error');
          this.syncInitializing.set(false); // Limpiar estado de inicialización
        } else if (update.currentFile === 'Cancelado' || update.status === 'cancelled') {
          this.oneDriveSyncService.syncStatus.set('cancelled');
          this.syncInitializing.set(false); // Limpiar estado de inicialización
        } else if (update.status === 'paused') {
          this.oneDriveSyncService.syncStatus.set('paused');
          // Si hay un mensaje de error de token, mostrarlo
          if (update.message && (update.message.includes('token') || update.message.includes('autenticación') || update.message.includes('authentication'))) {
            this.oneDriveSyncService.syncMessage.set(update.message);
            this.syncResult.set({
              success: false,
              message: update.message
            });
          }
        } else if (update.currentFile || update.currentFileName || update.status === 'syncing') {
          this.oneDriveSyncService.syncStatus.set('syncing');
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
      // Al reconectar, verificar estado de sincronización
      this.checkSyncStatus();
    });

    this.hubConnection.start()
      .then(() => {
        console.log('✅ SignalR conectado correctamente. ConnectionId:', this.hubConnection?.connectionId);
        // Verificar estado después de conectar
        this.checkSyncStatus();
      })
      .catch(err => {
        console.error('❌ Error conectando SignalR:', err);
      });
  }

  async syncNow(): Promise<void> {
    // Sincronizar todas las fuentes habilitadas
    if (this.syncingSourceId()) {
      console.warn('Ya hay una sincronización en progreso. Ignorando clic duplicado.');
      return;
    }

    // Inicializar estado de sincronización inmediatamente
    this.syncInitializing.set(true);
    this.syncResult.set(null);
    this.syncCancelled = false;
    this.syncResults.set({}); // Limpiar resultados previos
    this.oneDriveSyncService.syncStatus.set('syncing');
    this.oneDriveSyncService.syncMessage.set('Iniciando sincronización de todas las fuentes...');
    this.currentSyncStage.set('initializing');
    
    try {
      const results = await this.invoiceSourcesService.syncAll(this.forceReprocess());
      
      this.syncResults.set(results);
      
      // Recargar datos para actualizar estadísticas y historial
      await Promise.all([
        this.oneDriveSyncService.loadSyncedFiles(),
        this.invoiceSourcesService.loadAll(),
        this.loadSyncPreview()
      ]);
      
      const totalProcessed = Object.values(results).reduce((sum, r) => sum + r.processedCount, 0);
      const totalFailed = Object.values(results).reduce((sum, r) => sum + r.failedCount, 0);
      const totalSkipped = Object.values(results).reduce((sum, r) => sum + r.skippedCount, 0);
      const allSuccess = Object.values(results).every(r => r.success);
      
      const message = allSuccess
        ? `Sincronización completada: ${totalProcessed} procesados, ${totalFailed} fallidos, ${totalSkipped} omitidos`
        : `Sincronización completada con errores: ${totalProcessed} procesados, ${totalFailed} fallidos`;
      
      this.syncResult.set({
        success: allSuccess,
        message
      });
      
      if (this.forceReprocess()) {
        this.forceReprocess.set(false);
      }
      
      // Limpiar resultados después de 10 segundos
      setTimeout(() => {
        this.syncResults.set({});
      }, 10000);
    } catch (err: any) {
      if (!this.syncCancelled) {
        const errorMessage = err.error?.error || err.error?.errorMessage || err.message || 'Error durante la sincronización';
        this.syncResult.set({
          success: false,
          message: errorMessage
        });
      }
    } finally {
      this.syncInitializing.set(false);
    }
  }
  
  // Mantener método legacy para OneDrive específico (opcional)
  async syncOneDriveNow(): Promise<void> {
    if (this.oneDriveSyncService.syncing()) {
      console.warn('Ya hay una sincronización en progreso. Ignorando clic duplicado.');
      return;
    }

    this.syncResult.set(null);
    this.oneDriveSyncService.syncStatus.set('syncing');
    this.oneDriveSyncService.syncProgress.set(0);
    this.syncCancelled = false;
    
    try {
      const result = await this.oneDriveSyncService.syncNow(this.forceReprocess());
      
      if (this.syncCancelled) {
        return;
      }
      
      if (result.success) {
        const parts = [];
        if (result.processedCount > 0) parts.push(`${result.processedCount} procesados`);
        if (result.alreadySynced > 0) parts.push(`${result.alreadySynced} ya sincronizados`);
        if (result.alreadyExisted > 0) parts.push(`${result.alreadyExisted} ya en BD`);
        if (result.failedCount > 0) parts.push(`${result.failedCount} fallidos`);
        
        const message = parts.length > 0 
          ? `Sincronización completada: ${parts.join(', ')}` 
          : 'Sincronización completada sin cambios';
        
        // Este método es para OneDrive específico (legacy)
        this.syncResult.set({
          success: result.failedCount === 0,
          message,
          detailedErrors: result.detailedErrors
        });
        this.oneDriveSyncService.syncStatus.set('completed');
        
        // Recargar preview después de sincronizar
        await this.loadSyncPreview();
        
        if (this.forceReprocess()) {
          this.forceReprocess.set(false);
        }
      } else {
        this.syncResult.set({
          success: false,
          message: result.errorMessage || 'Error durante la sincronización',
          detailedErrors: result.detailedErrors
        });
        this.oneDriveSyncService.syncStatus.set('error');
      }
    } catch (err: any) {
      if (!this.syncCancelled) {
        const errorMessage = err.error?.error || err.error?.errorMessage || err.message || 'Error durante la sincronización';
        this.syncResult.set({
          success: false,
          message: errorMessage
        });
        this.oneDriveSyncService.syncStatus.set('error');
      }
    }
  }

  cancelSync(): void {
    // Solo funciona para OneDrive actualmente
    this.oneDriveSyncService.cancelSync();
    this.syncCancelled = true;
  }

  pauseSync(): void {
    // Solo funciona para OneDrive actualmente
    this.oneDriveSyncService.pauseSync();
  }

  resumeSync(): void {
    // Solo funciona para OneDrive actualmente
    this.oneDriveSyncService.resumeSync();
  }

  async viewFile(file: SyncedFile): Promise<void> {
    if (!file.invoiceId) {
      alert('Este archivo no tiene una factura asociada para visualizar.');
      return;
    }

    try {
      const blob = await firstValueFrom(
        this.http.get(`/api/invoices/${file.invoiceId}/download`, {
          responseType: 'blob'
        })
      );
      
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 100);
    } catch (err: any) {
      console.error('Error abriendo archivo:', err);
      alert('Error al abrir el archivo: ' + (err.message || 'Error desconocido'));
    }
  }

  applyFilterGlobal(event: Event, stringVal: string): void {
    this.table.filterGlobal((event.target as HTMLInputElement).value, stringVal);
  }

  filterByInvoice(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === 'with') {
      this.table.filter('true', 'hasInvoice', 'equals');
    } else if (value === 'without') {
      this.table.filter('false', 'hasInvoice', 'equals');
    } else {
      this.table.filter(null, 'hasInvoice', 'equals');
    }
  }

  filterByDate(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    if (value) {
      // Convertir a fecha y filtrar por día
      this.table.filter(value, 'syncedAtDate', 'equals');
    } else {
      this.table.filter(null, 'syncedAtDate', 'equals');
    }
  }

  showErrorDetails(file: SyncedFile): void {
    this.selectedFileForError.set(file);
    this.errorDialogVisible.set(true);
  }

  closeErrorDialog(): void {
    this.errorDialogVisible.set(false);
    this.selectedFileForError.set(null);
  }

  getErrorSummary(file: SyncedFile): string {
    if (file.status === 'Completed') {
      return 'Procesado correctamente';
    }
    if (file.status === 'Skipped') {
      return file.errorMessage || 'Archivo omitido (ya procesado o no es factura)';
    }
    if (file.status === 'Failed') {
      return file.errorMessage || 'Error desconocido durante el procesamiento';
    }
    return 'Sin información';
  }

  // Transformar datos para filtros
  getFilterableFiles(): any[] {
    return this.syncedFiles().map(f => ({
      ...f,
      hasInvoice: f.invoiceId ? 'true' : 'false',
      syncedAtDate: new Date(f.syncedAt).toISOString().split('T')[0],
      sourceType: this.extractSourceType(f.source)
    }));
  }

  // Extraer el tipo de fuente del campo source (ej: "OneDrive-{id}" -> "OneDrive")
  extractSourceType(source?: string): string {
    if (!source) return 'OneDrive'; // Por defecto, asumir OneDrive para compatibilidad
    const parts = source.split('-');
    return parts[0] || 'OneDrive';
  }
  
  // Obtener etiqueta de la etapa de sincronización
  getStageLabel(stage: string): string {
    const stageLabels: Record<string, string> = {
      'initializing': 'Inicializando...',
      'listing': 'Listando archivos...',
      'downloading': 'Descargando...',
      'processing': 'Procesando...',
      'extracting': 'Extrayendo datos...',
      'saving': 'Guardando...',
      'completed': 'Completado'
    };
    return stageLabels[stage] || stage;
  }
  
  // Obtener icono del estado del archivo
  getFileStatusIcon(status: string | null): string {
    if (!status) return 'pi-file';
    const iconMap: Record<string, string> = {
      'downloading': 'pi-download',
      'processing': 'pi-spin pi-spinner',
      'extracting': 'pi-cog',
      'saving': 'pi-save',
      'completed': 'pi-check-circle',
      'failed': 'pi-times-circle',
      'skipped': 'pi-minus-circle'
    };
    return iconMap[status] || 'pi-file';
  }
  
  // Obtener etiqueta del estado del archivo
  getFileStatusLabel(status: string): string {
    const labelMap: Record<string, string> = {
      'downloading': 'Descargando',
      'processing': 'Procesando',
      'extracting': 'Extrayendo datos',
      'saving': 'Guardando',
      'completed': 'Completado',
      'failed': 'Fallido',
      'skipped': 'Omitido'
    };
    return labelMap[status] || status;
  }

  getSeverity(status: string): 'success' | 'warning' | 'danger' | 'info' {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'Failed':
        return 'danger';
      case 'Skipped':
        return 'warning';
      default:
        return 'info';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Completed':
        return 'Completado';
      case 'Failed':
        return 'Fallido';
      case 'Skipped':
        return 'Omitido';
      default:
        return status;
    }
  }
}

