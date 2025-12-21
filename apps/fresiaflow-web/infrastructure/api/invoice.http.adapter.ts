import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { InvoiceApiPort } from '../../ports/invoice.api.port';
import { Invoice } from '../../domain/invoice.model';
import { firstValueFrom } from 'rxjs';

/**
 * Adapter HTTP para el puerto de facturas.
 */
@Injectable({ providedIn: 'root' })
export class InvoiceHttpAdapter implements InvoiceApiPort {
  private readonly baseUrl = '/api/invoices';

  constructor(private http: HttpClient) {}

  async getAllInvoices(): Promise<Invoice[]> {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:16',message:'getAllInvoices entry',data:{baseUrl:this.baseUrl},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'D'})}).catch(()=>{});
    // #endregion
    try {
      const response = await firstValueFrom(
        this.http.get<Invoice[]>(this.baseUrl)
      );
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:20',message:'getAllInvoices response received',data:{count:response?.length,firstItem:response?.[0]},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'D'})}).catch(()=>{});
      // #endregion
      const mapped = response.map(this.mapToDomain);
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:24',message:'getAllInvoices mapped',data:{count:mapped.length,firstItemId:mapped[0]?.id},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
      // #endregion
      return mapped;
    } catch (error: any) {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:28',message:'getAllInvoices error',data:{error:error?.message,status:error?.status,statusText:error?.statusText},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'D'})}).catch(()=>{});
      // #endregion
      throw error;
    }
  }

  async getInvoiceById(id: string): Promise<Invoice> {
    const response = await firstValueFrom(
      this.http.get<Invoice>(`${this.baseUrl}/${id}`)
    );
    return this.mapToDomain(response);
  }

  async uploadInvoice(file: File): Promise<Invoice> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await firstValueFrom(
      this.http.post<Invoice>(`${this.baseUrl}/upload`, formData)
    );
    return this.mapToDomain(response);
  }

  async markAsPaid(invoiceId: string, transactionId: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/${invoiceId}/mark-paid`, { transactionId })
    );
  }

  async deleteInvoice(id: string): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${id}`)
    );
  }

  private mapToDomain(dto: any): Invoice {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:52',message:'mapToDomain entry',data:{dtoKeys:Object.keys(dto),dtoAmount:dto.amount,dtoIssueDate:dto.issueDate},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
    // #endregion
    try {
      const result = {
        ...dto,
        issueDate: new Date(dto.issueDate),
        dueDate: dto.dueDate ? new Date(dto.dueDate) : undefined,
        createdAt: new Date(dto.createdAt),
        reconciledAt: dto.reconciledAt ? new Date(dto.reconciledAt) : undefined
      };
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:61',message:'mapToDomain success',data:{resultAmount:result.amount,resultIssueDate:result.issueDate},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
      // #endregion
      return result;
    } catch (error: any) {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:67',message:'mapToDomain error',data:{error:error?.message,dto},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
      // #endregion
      throw error;
    }
  }
}

