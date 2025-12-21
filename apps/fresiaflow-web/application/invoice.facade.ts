import { Injectable, signal, computed, Inject } from '@angular/core';
import { Invoice, InvoiceStatus } from '../domain/invoice.model';
import { InvoiceApiPort } from '../ports/invoice.api.port';
import { INVOICE_API_PORT } from '../infrastructure/api/providers';

/**
 * Facade para gestión de facturas.
 * Gestiona el estado usando signals.
 */
@Injectable({ providedIn: 'root' })
export class InvoiceFacade {
  private _invoices = signal<Invoice[]>([]);
  private _loading = signal<boolean>(false);
  private _error = signal<string | null>(null);

  readonly invoices = this._invoices.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly pendingInvoices = computed(() =>
    this._invoices().filter(i => i.status === InvoiceStatus.Pending)
  );

  readonly overdueInvoices = computed(() =>
    this._invoices().filter(i => i.status === InvoiceStatus.Overdue)
  );

  readonly paidInvoices = computed(() =>
    this._invoices().filter(i => i.status === InvoiceStatus.Paid)
  );

  constructor(@Inject(INVOICE_API_PORT) private invoiceApi: InvoiceApiPort) {}

  async loadInvoices(): Promise<void> {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.facade.ts:34',message:'loadInvoices entry',data:{},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'D'})}).catch(()=>{});
    // #endregion
    this._loading.set(true);
    this._error.set(null);

    try {
      const invoices = await this.invoiceApi.getAllInvoices();
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.facade.ts:40',message:'loadInvoices success',data:{count:invoices.length},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'D'})}).catch(()=>{});
      // #endregion
      this._invoices.set(invoices);
    } catch (error: unknown) {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoice.facade.ts:44',message:'loadInvoices error',data:{error:error instanceof Error ? error.message : String(error)},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'D'})}).catch(()=>{});
      // #endregion
      this._error.set(error instanceof Error ? error.message : 'Error al cargar facturas');
    } finally {
      this._loading.set(false);
    }
  }

  async uploadInvoice(file: File): Promise<Invoice> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const result = await this.invoiceApi.uploadInvoice(file);
      this._invoices.update(invoices => [...invoices, result]);
      return result;
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al subir factura');
      throw error;
    } finally {
      this._loading.set(false);
    }
  }

  async markAsPaid(invoiceId: string, transactionId: string): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      await this.invoiceApi.markAsPaid(invoiceId, transactionId);
      this._invoices.update(invoices =>
        invoices.map(i => i.id === invoiceId
          ? { ...i, status: InvoiceStatus.Paid, reconciledWithTransactionId: transactionId }
          : i)
      );
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al marcar como pagada');
    } finally {
      this._loading.set(false);
    }
  }
}

