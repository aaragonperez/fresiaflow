import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface OneDriveSyncConfig {
  configured: boolean;
  enabled: boolean;
  tenantId?: string;
  clientId?: string;
  hasClientSecret: boolean;
  folderPath?: string;
  driveId?: string;
  syncIntervalMinutes: number;
  lastSyncAt?: string;
  lastSyncError?: string;
  totalFilesSynced: number;
}

export interface OneDriveSyncConfigUpdate {
  enabled: boolean;
  tenantId: string;
  clientId: string;
  clientSecret: string;
  folderPath: string;
  driveId?: string;
  syncIntervalMinutes: number;
}

export interface SyncResult {
  success: boolean;
  processedCount: number;
  skippedCount: number;
  failedCount: number;
  totalDetected: number;
  alreadyExisted: number;
  alreadySynced: number;
  errorMessage?: string;
  detailedErrors?: string[];
  filesByType?: Record<string, number>;
}

export interface SyncPreview {
  totalFiles: number;
  supportedFiles: number;
  unsupportedFiles: number;
  alreadySynced: number;
  alreadyExistsInDb: number;
  pendingToProcess: number;
  filesByExtension: Record<string, number>;
  unsupportedExtensions: string[];
  errorMessage?: string;
}

export interface SyncedFile {
  id: string;
  fileName: string;
  filePath: string;
  fileSize: number;
  syncedAt: string;
  status: string;
  errorMessage?: string;
  invoiceId?: string;
  source?: string; // "OneDrive-{id}", "Email-{id}", "Portal-{id}", etc.
}

export interface FolderValidationResult {
  isValid: boolean;
  folderPath?: string;
  fileCount: number;
  invoiceFileCount: number;
  errorMessage?: string;
}

@Injectable({
  providedIn: 'root'
})
export class OneDriveSyncService {
  private readonly baseUrl = '/api/sync/onedrive';
  
  config = signal<OneDriveSyncConfig | null>(null);
  syncedFiles = signal<SyncedFile[]>([]);
  loading = signal(false);
  syncing = signal(false);
  error = signal<string | null>(null);

  // Estado de progreso de sincronización (persiste entre navegaciones)
  syncProgress = signal(0);
  syncCurrentFile = signal('');
  syncProcessedCount = signal(0);
  syncTotalCount = signal(0);
  syncStatus = signal<'idle' | 'syncing' | 'completed' | 'error' | 'cancelled' | 'paused'>('idle');
  syncMessage = signal('');

  constructor(private http: HttpClient) {}

  async loadConfig(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    
    try {
      const config = await firstValueFrom(
        this.http.get<OneDriveSyncConfig>(`${this.baseUrl}/config`)
      );
      this.config.set(config);
    } catch (err: any) {
      this.error.set(err.message || 'Error cargando configuración');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async saveConfig(config: OneDriveSyncConfigUpdate): Promise<OneDriveSyncConfig> {
    this.loading.set(true);
    this.error.set(null);
    
    try {
      const result = await firstValueFrom(
        this.http.post<OneDriveSyncConfig>(`${this.baseUrl}/config`, config)
      );
      this.config.set(result);
      return result;
    } catch (err: any) {
      this.error.set(err.message || 'Error guardando configuración');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async validateConfig(
    tenantId: string,
    clientId: string,
    clientSecret: string,
    folderPath: string,
    driveId?: string
  ): Promise<FolderValidationResult> {
    try {
      return await firstValueFrom(
        this.http.post<FolderValidationResult>(`${this.baseUrl}/validate`, {
          tenantId,
          clientId,
          clientSecret,
          folderPath,
          driveId
        })
      );
    } catch (err: any) {
      return {
        isValid: false,
        fileCount: 0,
        invoiceFileCount: 0,
        errorMessage: err.error?.errorMessage || err.message || 'Error validando configuración'
      };
    }
  }

  async getSyncPreview(): Promise<SyncPreview> {
    try {
      return await firstValueFrom(
        this.http.get<SyncPreview>(`${this.baseUrl}/sync/preview`)
      );
    } catch (err: any) {
      console.error('Error obteniendo preview de sincronización:', err);
      return {
        totalFiles: 0,
        supportedFiles: 0,
        unsupportedFiles: 0,
        alreadySynced: 0,
        alreadyExistsInDb: 0,
        pendingToProcess: 0,
        filesByExtension: {},
        unsupportedExtensions: [],
        errorMessage: err.error?.error || err.message || 'Error obteniendo preview'
      };
    }
  }

  async syncNow(forceReprocess: boolean = false): Promise<SyncResult> {
    this.syncing.set(true);
    this.error.set(null);
    
    try {
      const result = await firstValueFrom(
        this.http.post<SyncResult>(`${this.baseUrl}/sync`, { forceReprocess })
      );
      
      // Recargar configuración para obtener estadísticas actualizadas
      await this.loadConfig();
      await this.loadSyncedFiles();
      
      return result;
    } catch (err: any) {
      this.error.set(err.message || 'Error durante la sincronización');
      throw err;
    } finally {
      this.syncing.set(false);
    }
  }

  async cancelSync(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post(`${this.baseUrl}/sync/cancel`, {})
      );
      
      this.syncStatus.set('cancelled');
      this.syncMessage.set('Sincronización cancelada por el usuario');
      
      // Recargar datos después de un momento
      setTimeout(async () => {
        await this.loadConfig();
        await this.loadSyncedFiles();
      }, 2000);
    } catch (err: any) {
      console.error('Error cancelando sincronización:', err);
      this.error.set(err.error?.error || err.message || 'Error cancelando la sincronización');
    }
  }

  async pauseSync(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post(`${this.baseUrl}/sync/pause`, {})
      );
      
      this.syncStatus.set('paused');
      this.syncMessage.set('Sincronización pausada');
    } catch (err: any) {
      console.error('Error pausando sincronización:', err);
      this.error.set(err.error?.error || err.message || 'Error pausando la sincronización');
    }
  }

  async resumeSync(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post(`${this.baseUrl}/sync/resume`, {})
      );
      
      this.syncStatus.set('syncing');
      this.syncMessage.set('Sincronización reanudada');
    } catch (err: any) {
      console.error('Error reanudando sincronización:', err);
      this.error.set(err.error?.error || err.message || 'Error reanudando la sincronización');
    }
  }

  async getSyncStatus(): Promise<{ isSyncing: boolean }> {
    try {
      return await firstValueFrom(
        this.http.get<{ isSyncing: boolean }>(`${this.baseUrl}/sync/status`)
      );
    } catch (err: any) {
      console.error('Error obteniendo estado de sincronización:', err);
      return { isSyncing: false };
    }
  }

  async loadSyncedFiles(page = 1, pageSize = 50): Promise<void> {
    try {
      const files = await firstValueFrom(
        this.http.get<SyncedFile[]>(`${this.baseUrl}/files`, {
          params: { page: page.toString(), pageSize: pageSize.toString() }
        })
      );
      this.syncedFiles.set(files);
    } catch (err: any) {
      console.error('Error cargando archivos sincronizados:', err);
    }
  }

  async downloadFile(fileId: string): Promise<Blob> {
    return await firstValueFrom(
      this.http.get(`${this.baseUrl}/files/${fileId}/download`, {
        responseType: 'blob'
      })
    );
  }

  getFilePreviewUrl(fileId: string): string {
    return `${this.baseUrl}/files/${fileId}/download`;
  }

  async clearDatabase(confirmationCode: string): Promise<{ success: boolean; message: string }> {
    try {
      const result = await firstValueFrom(
        this.http.delete<{ success: boolean; message: string }>(`${this.baseUrl}/clear-database`, {
          body: {
            confirmed: true,
            confirmationCode: confirmationCode
          }
        })
      );
      
      // Recargar datos después de limpiar
      await this.loadConfig();
      await this.loadSyncedFiles();
      
      return result;
    } catch (err: any) {
      throw new Error(err.error?.error || err.message || 'Error limpiando la base de datos');
    }
  }
}


