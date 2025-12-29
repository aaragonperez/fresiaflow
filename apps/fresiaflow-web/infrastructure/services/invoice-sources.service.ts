import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';

export interface InvoiceSource {
  id: string;
  sourceType: 'Email' | 'Portal' | 'WebScraping' | 'OneDrive';
  name: string;
  enabled: boolean;
  lastSyncAt: string | null;
  lastSyncError: string | null;
  totalFilesSynced: number;
  createdAt: string;
  updatedAt: string;
}

export interface InvoiceSourceDetail extends InvoiceSource {
  configJson: string;
}

export interface CreateOrUpdateSourceRequest {
  id?: string;
  sourceType: 'Email' | 'Portal' | 'WebScraping' | 'OneDrive';
  name: string;
  configJson: string;
  enabled?: boolean;
}

export interface SyncPreview {
  totalFiles: number;
  supportedFiles: number;
  alreadySynced: number;
  pendingToProcess: number;
  errorMessage?: string;
}

export interface SyncResult {
  success: boolean;
  processedCount: number;
  failedCount: number;
  skippedCount: number;
  errorMessage?: string;
  detailedErrors: string[];
}

export interface SourceValidationResult {
  isValid: boolean;
  errorMessage?: string;
  info?: Record<string, any>;
}

@Injectable({
  providedIn: 'root'
})
export class InvoiceSourcesService {
  private http = inject(HttpClient);
  private apiUrl = '/api/invoice-sources';

  // State signals
  sources = signal<InvoiceSource[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  async loadAll(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await firstValueFrom(
        this.http.get<any[]>(this.apiUrl)
      );
      // Mapear sourceType de número a string
      const mappedSources: InvoiceSource[] = response.map(source => ({
        ...source,
        sourceType: this.mapSourceTypeFromNumber(source.sourceType)
      }));
      this.sources.set(mappedSources);
    } catch (err: any) {
      this.error.set(err.message || 'Error cargando fuentes');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  private mapSourceTypeFromNumber(sourceType: number | string): 'Email' | 'Portal' | 'WebScraping' | 'OneDrive' {
    // Si ya es string, devolverlo
    if (typeof sourceType === 'string') {
      return sourceType as 'Email' | 'Portal' | 'WebScraping' | 'OneDrive';
    }
    
    // Mapear número a string (Email=0, Portal=1, WebScraping=2, OneDrive=3)
    const numberToTypeMap: Record<number, 'Email' | 'Portal' | 'WebScraping' | 'OneDrive'> = {
      0: 'Email',
      1: 'Portal',
      2: 'WebScraping',
      3: 'OneDrive'
    };
    
    return numberToTypeMap[sourceType] || 'Email';
  }

  async getById(id: string): Promise<InvoiceSourceDetail> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await firstValueFrom(
        this.http.get<any>(`${this.apiUrl}/${id}`)
      );
      // Mapear sourceType de número a string
      return {
        ...response,
        sourceType: this.mapSourceTypeFromNumber(response.sourceType)
      };
    } catch (err: any) {
      this.error.set(err.message || 'Error obteniendo fuente');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async createOrUpdate(request: CreateOrUpdateSourceRequest): Promise<InvoiceSource> {
    this.loading.set(true);
    this.error.set(null);
    try {
      // Mapear string a número del enum (Email=0, Portal=1, WebScraping=2, OneDrive=3)
      const sourceTypeMap: Record<string, number> = {
        'Email': 0,
        'Portal': 1,
        'WebScraping': 2,
        'OneDrive': 3
      };
      
      const requestBody = {
        ...request,
        sourceType: sourceTypeMap[request.sourceType] ?? 0
      };
      
      const response = await firstValueFrom(
        this.http.post<any>(this.apiUrl, requestBody)
      );
      // Mapear sourceType de número a string
      const mappedResponse: InvoiceSource = {
        ...response,
        sourceType: this.mapSourceTypeFromNumber(response.sourceType)
      };
      await this.loadAll(); // Refresh list
      return mappedResponse;
    } catch (err: any) {
      this.error.set(err.message || 'Error guardando fuente');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async delete(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.http.delete(`${this.apiUrl}/${id}`)
      );
      await this.loadAll(); // Refresh list
    } catch (err: any) {
      this.error.set(err.message || 'Error eliminando fuente');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async sync(id: string, forceReprocess = false): Promise<SyncResult> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.post<SyncResult>(`${this.apiUrl}/${id}/sync?forceReprocess=${forceReprocess}`, {})
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error sincronizando');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async getPreview(id: string): Promise<SyncPreview> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.get<SyncPreview>(`${this.apiUrl}/${id}/preview`)
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error obteniendo preview');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async validate(sourceType: 'Email' | 'Portal' | 'WebScraping' | 'OneDrive', configJson: string): Promise<SourceValidationResult> {
    this.loading.set(true);
    this.error.set(null);
    try {
      // Mapear string a número del enum (Email=0, Portal=1, WebScraping=2, OneDrive=3)
      const sourceTypeMap: Record<string, number> = {
        'Email': 0,
        'Portal': 1,
        'WebScraping': 2,
        'OneDrive': 3
      };
      
      return await firstValueFrom(
        this.http.post<SourceValidationResult>(`${this.apiUrl}/validate`, {
          sourceType: sourceTypeMap[sourceType] ?? 0,
          configJson
        })
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error validando configuración');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async syncAll(forceReprocess = false): Promise<Record<string, SyncResult>> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.post<Record<string, SyncResult>>(`${this.apiUrl}/sync-all?forceReprocess=${forceReprocess}`, {})
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error sincronizando todas las fuentes');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  async resumeSync(sourceId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.http.post(`${this.apiUrl}/${sourceId}/sync/resume`, {})
      );
    } catch (err: any) {
      this.error.set(err.error?.error || err.message || 'Error reanudando sincronización');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }
}

