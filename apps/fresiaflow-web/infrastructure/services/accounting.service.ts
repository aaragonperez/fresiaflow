import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';

export interface AccountingEntry {
  id: string;
  entryNumber?: number;
  entryYear: number;
  entryDate: string;
  description: string;
  reference?: string;
  invoiceId?: string;
  source: 'Automatic' | 'Manual';
  status: 'Draft' | 'Posted' | 'Reversed';
  isReversed: boolean;
  reversedByEntryId?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
  lines: AccountingEntryLine[];
  totalDebit: number;
  totalCredit: number;
  isBalanced: boolean;
}

export interface AccountingEntryLine {
  id: string;
  accountingAccountId: string;
  side: 'Debit' | 'Credit';
  amount: number;
  currency: string;
  description?: string;
}

export interface AccountingAccount {
  id: string;
  code: string;
  name: string;
  type: 'Asset' | 'Liability' | 'Equity' | 'Income' | 'Expense';
  isActive: boolean;
}

export interface FailedInvoice {
  invoiceId: string;
  invoiceNumber: string;
  supplierName: string;
  reason: string;
}

export interface GenerateEntriesResult {
  totalProcessed: number;
  successCount: number;
  errorCount: number;
  errors: string[];
  failedInvoices: FailedInvoice[];
}

export interface PostBalancedEntriesResult {
  totalProcessed: number;
  successCount: number;
  errorCount: number;
  errors: string[];
}

export interface UpdateEntryRequest {
  description?: string;
  entryDate?: string;
  lines?: AccountingEntryLine[];
  notes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AccountingService {
  private http = inject(HttpClient);
  private apiUrl = '/api/accounting';

  // State signals
  entries = signal<AccountingEntry[]>([]);
  accounts = signal<AccountingAccount[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  /**
   * Carga todos los asientos contables con filtros opcionales.
   */
  async loadEntries(filters?: {
    startDate?: string;
    endDate?: string;
    status?: 'Draft' | 'Posted' | 'Reversed';
    source?: 'Automatic' | 'Manual';
    invoiceId?: string;
  }): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const params: any = {};
      if (filters?.startDate) params.startDate = filters.startDate;
      if (filters?.endDate) params.endDate = filters.endDate;
      if (filters?.status) params.status = filters.status;
      if (filters?.source) params.source = filters.source;
      if (filters?.invoiceId) params.invoiceId = filters.invoiceId;

      const response = await firstValueFrom(
        this.http.get<any[]>(`${this.apiUrl}/entries`, { params })
      );
      // Mapear enums desde números/strings
      const mappedEntries: AccountingEntry[] = response.map(entry => ({
        ...entry,
        source: this.mapSource(entry.source),
        status: this.mapStatus(entry.status),
        lines: entry.lines.map((line: any) => ({
          ...line,
          side: this.mapSide(line.side)
        }))
      }));
      this.entries.set(mappedEntries);
    } catch (err: any) {
      this.error.set(err.message || 'Error cargando asientos contables');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  private mapSource(source: number | string): 'Automatic' | 'Manual' {
    if (typeof source === 'number') {
      return source === 1 ? 'Automatic' : 'Manual';
    }
    return source === 'Automatic' || source === '1' ? 'Automatic' : 'Manual';
  }

  private mapStatus(status: number | string): 'Draft' | 'Posted' | 'Reversed' {
    if (typeof status === 'number') {
      switch (status) {
        case 1: return 'Draft';
        case 2: return 'Posted';
        case 3: return 'Reversed';
        default: return 'Draft';
      }
    }
    return status as 'Draft' | 'Posted' | 'Reversed';
  }

  private mapSide(side: number | string): 'Debit' | 'Credit' {
    if (typeof side === 'number') {
      return side === 1 ? 'Debit' : 'Credit';
    }
    return side === 'Debit' || side === '1' ? 'Debit' : 'Credit';
  }

  /**
   * Obtiene un asiento contable por ID.
   */
  async getEntry(id: string): Promise<AccountingEntry> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.get<AccountingEntry>(`${this.apiUrl}/entries/${id}`)
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error obteniendo asiento contable');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Carga todas las cuentas contables.
   */
  async loadAccounts(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await firstValueFrom(
        this.http.get<any[]>(`${this.apiUrl}/accounts`)
      );
      // Mapear el tipo de cuenta desde string a enum
      const mappedAccounts: AccountingAccount[] = response.map(acc => ({
        ...acc,
        type: this.mapAccountType(acc.type)
      }));
      this.accounts.set(mappedAccounts);
    } catch (err: any) {
      this.error.set(err.message || 'Error cargando cuentas contables');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  private mapAccountType(type: string): 'Asset' | 'Liability' | 'Equity' | 'Income' | 'Expense' {
    switch (type) {
      case 'Asset': return 'Asset';
      case 'Liability': return 'Liability';
      case 'Equity': return 'Equity';
      case 'Income': return 'Income';
      case 'Expense': return 'Expense';
      default: return 'Expense';
    }
  }

  /**
   * Genera asientos contables automáticamente para todas las facturas.
   */
  async generateEntries(): Promise<GenerateEntriesResult> {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'accounting.service.ts:201',message:'generateEntries service method called',data:{apiUrl:this.apiUrl,endpoint:`${this.apiUrl}/entries/generate`},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'F'})}).catch(()=>{});
    // #endregion
    
    this.loading.set(true);
    this.error.set(null);
    try {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'accounting.service.ts:206',message:'HTTP POST request starting',data:{url:`${this.apiUrl}/entries/generate`},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'F'})}).catch(()=>{});
      // #endregion
      
      const result = await firstValueFrom(
        this.http.post<GenerateEntriesResult>(`${this.apiUrl}/entries/generate`, {})
      );
      
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'accounting.service.ts:209',message:'HTTP POST request completed',data:{totalProcessed:result.totalProcessed,successCount:result.successCount,errorCount:result.errorCount},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'F'})}).catch(()=>{});
      // #endregion
      
      return result;
    } catch (err: any) {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'accounting.service.ts:211',message:'HTTP POST request error',data:{errorMessage:err?.message,errorStatus:err?.status,errorUrl:err?.url},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'F'})}).catch(()=>{});
      // #endregion
      
      this.error.set(err.message || 'Error generando asientos contables');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Genera un asiento contable para una factura específica.
   */
  async generateEntryForInvoice(invoiceId: string): Promise<AccountingEntry> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.post<AccountingEntry>(`${this.apiUrl}/entries/generate/${invoiceId}`, {})
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error generando asiento contable');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Actualiza un asiento contable.
   */
  async updateEntry(id: string, request: UpdateEntryRequest): Promise<AccountingEntry> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.put<AccountingEntry>(`${this.apiUrl}/entries/${id}`, request)
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error actualizando asiento contable');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Contabiliza (post) un asiento.
   */
  async postEntry(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.http.post(`${this.apiUrl}/entries/${id}/post`, {})
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error contabilizando asiento');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Contabiliza todos los asientos balanceados en estado Draft.
   */
  async postAllBalancedEntries(): Promise<PostBalancedEntriesResult> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.post<PostBalancedEntriesResult>(`${this.apiUrl}/entries/post-balanced`, {})
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error contabilizando asientos balanceados');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Crea o actualiza una cuenta contable.
   */
  async createOrUpdateAccount(account: {
    id?: string;
    code: string;
    name: string;
    type: 'Asset' | 'Liability' | 'Equity' | 'Income' | 'Expense';
    isActive?: boolean;
  }): Promise<AccountingAccount> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await firstValueFrom(
        this.http.post<any>(`${this.apiUrl}/accounts`, account)
      );
      const mapped = {
        ...response,
        type: this.mapAccountType(response.type)
      };
      // Actualizar cache
      const accounts = this.accounts();
      const index = accounts.findIndex(a => a.id === mapped.id);
      if (index >= 0) {
        accounts[index] = mapped;
      } else {
        accounts.push(mapped);
      }
      this.accounts.set([...accounts]);
      return mapped;
    } catch (err: any) {
      this.error.set(err.message || 'Error guardando cuenta contable');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Elimina (desactiva) una cuenta contable.
   */
  async deleteAccount(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.http.delete(`${this.apiUrl}/accounts/${id}`)
      );
      // Actualizar cache
      const accounts = this.accounts().filter(a => a.id !== id);
      this.accounts.set(accounts);
    } catch (err: any) {
      this.error.set(err.message || 'Error eliminando cuenta contable');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Regenera todos los asientos contables automáticamente.
   */
  async regenerateAllEntries(): Promise<GenerateEntriesResult> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.post<GenerateEntriesResult>(`${this.apiUrl}/entries/regenerate`, {})
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error regenerando asientos contables');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Regenera asientos contables seleccionados.
   */
  async regenerateSelectedEntries(entryIds: string[]): Promise<GenerateEntriesResult> {
    this.loading.set(true);
    this.error.set(null);
    try {
      return await firstValueFrom(
        this.http.post<GenerateEntriesResult>(`${this.apiUrl}/entries/regenerate/selected`, { entryIds })
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error regenerando asientos contables');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Cancela la operación de generación/regeneración en progreso.
   */
  async cancelGeneration(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post(`${this.apiUrl}/entries/generation/cancel`, {})
      );
    } catch (err: any) {
      this.error.set(err.message || 'Error cancelando generación');
      throw err;
    }
  }

  /**
   * Obtiene las facturas que no pudieron generar asientos.
   */
  async getFailedInvoices(): Promise<FailedInvoice[]> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await firstValueFrom(
        this.http.get<FailedInvoice[]>(`${this.apiUrl}/failed-invoices`)
      );
      return response;
    } catch (err: any) {
      this.error.set(err.message || 'Error obteniendo facturas fallidas');
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Obtiene una factura por ID (para verla desde contabilidad).
   */
  async getInvoice(invoiceId: string): Promise<any> {
    try {
      const response = await firstValueFrom(
        this.http.get<any>(`${this.apiUrl}/invoices/${invoiceId}`)
      );
      return response;
    } catch (err: any) {
      this.error.set(err.message || 'Error obteniendo factura');
      throw err;
    }
  }
}

