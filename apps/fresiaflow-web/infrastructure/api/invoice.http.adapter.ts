import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { InvoiceApiPort, UpdateInvoiceRequest, InvoiceFilter } from '../../ports/invoice.api.port';
import { Invoice } from '../../domain/invoice.model';
import { firstValueFrom } from 'rxjs';

/**
 * Adapter HTTP para el puerto de facturas.
 */
@Injectable({ providedIn: 'root' })
export class InvoiceHttpAdapter implements InvoiceApiPort {
  private readonly baseUrl = '/api/invoices';

  constructor(private http: HttpClient) {}

  async getAllInvoices(filter?: InvoiceFilter): Promise<Invoice[]> {
    try {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:16',message:'getAllInvoices entry',data:{filter},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
      // #endregion
      let params = new HttpParams();
      if (filter?.year) params = params.set('year', filter.year.toString());
      if (filter?.quarter) params = params.set('quarter', filter.quarter.toString());
      if (filter?.supplierName) params = params.set('supplierName', filter.supplierName);
      if (filter?.paymentType) params = params.set('paymentType', filter.paymentType);

      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:22',message:'getAllInvoices params built',data:{paramsString:params.toString(),hasYear:!!filter?.year,hasQuarter:!!filter?.quarter,hasSupplier:!!filter?.supplierName,hasPaymentType:!!filter?.paymentType},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
      // #endregion
      console.log('InvoiceHttpAdapter.getAllInvoices - Filtro:', filter);
      console.log('InvoiceHttpAdapter.getAllInvoices - Params:', params.toString());

      const response = await firstValueFrom(
        this.http.get<Invoice[]>(this.baseUrl, { params })
      );
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:30',message:'getAllInvoices response received',data:{invoiceCount:response.length},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'D'})}).catch(()=>{});
      // #endregion
      console.log('InvoiceHttpAdapter.getAllInvoices - Respuesta:', response.length, 'facturas');
      return response.map(this.mapToDomain.bind(this));
    } catch (error: any) {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.http.adapter.ts:33',message:'getAllInvoices error',data:{errorMessage:error?.message,errorStack:error?.stack},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
      // #endregion
      console.error('InvoiceHttpAdapter.getAllInvoices - Error:', error);
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

    try {
      const response = await firstValueFrom(
        this.http.post<Invoice>(`${this.baseUrl}/upload`, formData)
      );
      return this.mapToDomain(response);
    } catch (error: any) {
      // Extraer mensaje de error del backend
      let errorMessage = 'Error al procesar la factura';
      
      if (error?.error?.message) {
        errorMessage = error.error.message;
      } else if (error?.error?.title) {
        errorMessage = error.error.title;
      } else if (error?.error && typeof error.error === 'string') {
        errorMessage = error.error;
      } else if (error?.message) {
        errorMessage = error.message;
      }

      // Traducir errores técnicos a mensajes amigables
      errorMessage = this.translateErrorMessage(errorMessage, file.name);
      
      const uploadError = new Error(errorMessage);
      (uploadError as any).fileName = file.name;
      throw uploadError;
    }
  }

  /**
   * Traduce mensajes de error técnicos a mensajes amigables para el usuario.
   */
  private translateErrorMessage(errorMessage: string, fileName: string): string {
    const translations: Record<string, string> = {
      'El número de factura no puede estar vacío': 
        `No se pudo detectar el número de factura en "${fileName}". Verifica que el documento sea una factura válida.`,
      'El total de la factura debe ser mayor que cero': 
        `No se pudo detectar el importe total en "${fileName}". Verifica que el documento sea legible.`,
      'La moneda no puede estar vacía': 
        `No se pudo detectar la moneda en "${fileName}".`,
      'Error extrayendo datos de la factura': 
        `No se pudieron extraer los datos de "${fileName}". El documento podría no ser una factura o estar ilegible.`,
    };

    // Buscar traducciones parciales
    for (const [key, value] of Object.entries(translations)) {
      if (errorMessage.includes(key)) {
        return value;
      }
    }

    // Detectar errores de factura duplicada
    if (errorMessage.includes('Ya existe una factura con el número')) {
      const match = errorMessage.match(/Ya existe una factura con el número (.+)/);
      if (match) {
        return `La factura "${match[1]}" ya existe en el sistema. No se puede duplicar.`;
      }
    }

    // Si el error contiene información técnica, simplificarlo
    if (errorMessage.includes('Exception') || errorMessage.includes('Error:')) {
      return `Error al procesar "${fileName}". Por favor, verifica que el archivo sea una factura válida y vuelve a intentarlo.`;
    }

    return `${errorMessage} (${fileName})`;
  }

  async updateInvoice(id: string, data: UpdateInvoiceRequest): Promise<Invoice> {
    const response = await firstValueFrom(
      this.http.put<Invoice>(`${this.baseUrl}/${id}`, data)
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

  async exportToExcel(filter?: InvoiceFilter): Promise<Blob> {
    let params = new HttpParams();
    if (filter?.year) params = params.set('year', filter.year.toString());
    if (filter?.quarter) params = params.set('quarter', filter.quarter.toString());
    if (filter?.supplierName) params = params.set('supplierName', filter.supplierName);
    if (filter?.paymentType) params = params.set('paymentType', filter.paymentType);

    const response = await firstValueFrom(
      this.http.get(`${this.baseUrl}/export`, { 
        params,
        responseType: 'blob'
      })
    );
    return response;
  }

  async chatAboutInvoices(question: string, filter?: InvoiceFilter): Promise<{ answer: string; context?: any }> {
    let params = new HttpParams();
    if (filter?.year) params = params.set('year', filter.year.toString());
    if (filter?.quarter) params = params.set('quarter', filter.quarter.toString());
    if (filter?.supplierName) params = params.set('supplierName', filter.supplierName);
    if (filter?.paymentType) params = params.set('paymentType', filter.paymentType);

    const response = await firstValueFrom(
      this.http.post<{ answer: string; context?: any }>(`${this.baseUrl}/chat`, { question }, { params })
    );
    return response;
  }

  private mapToDomain(dto: any): Invoice {
    try {
      const result: Invoice = {
        id: dto.id,
        invoiceNumber: dto.invoiceNumber,
        issueDate: new Date(dto.issueDate),
        receivedDate: new Date(dto.receivedDate || dto.issueDate),
        supplierName: dto.supplierName,
        supplierTaxId: dto.supplierTaxId,
        supplierAddress: dto.supplierAddress,
        subtotalAmount: dto.subtotalAmount ?? 0,
        taxAmount: dto.taxAmount,
        taxRate: dto.taxRate,
        totalAmount: dto.totalAmount,
        currency: dto.currency || 'EUR',
        paymentType: dto.paymentType === 'Bank' ? 'Bank' as any : 'Cash' as any,
        payments: (dto.payments || []).map((p: any) => ({
          id: p.id,
          bankTransactionId: p.bankTransactionId,
          amount: p.amount,
          currency: p.currency || dto.currency || 'EUR',
          paymentDate: new Date(p.paymentDate)
        })),
        origin: dto.origin || 'ManualUpload' as any,
        originalFilePath: dto.originalFilePath,
        processedFilePath: dto.processedFilePath,
        extractionConfidence: dto.extractionConfidence,
        notes: dto.notes,
        lines: (dto.lines || []).map((line: any) => ({
          id: line.id,
          lineNumber: line.lineNumber,
          description: line.description,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
          unitPriceCurrency: line.unitPriceCurrency || dto.currency || 'EUR',
          taxRate: line.taxRate,
          lineTotal: line.lineTotal,
          lineTotalCurrency: line.lineTotalCurrency || dto.currency || 'EUR'
        })),
        createdAt: new Date(dto.createdAt || dto.issueDate),
        updatedAt: new Date(dto.updatedAt || dto.issueDate)
      };
      return result;
    } catch (error: any) {
      throw error;
    }
  }
}

