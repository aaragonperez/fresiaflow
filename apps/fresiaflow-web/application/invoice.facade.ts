import { Injectable, Inject, signal, computed } from '@angular/core';
import { InvoiceApiPort, InvoiceFilter } from '../ports/invoice.api.port';
import { Invoice, PaymentType } from '../domain/invoice.model';
import { INVOICE_API_PORT } from '../infrastructure/api/providers';

/**
 * Facade para gestión de facturas recibidas.
 * Refleja el modelo contable: todas las facturas están contabilizadas desde su recepción.
 */
@Injectable({ providedIn: 'root' })
export class InvoiceFacade {
  // Estado interno
  private readonly _invoices = signal<Invoice[]>([]);
  private readonly _loading = signal<boolean>(false);
  private readonly _error = signal<string | null>(null);
  private readonly _currentFilter = signal<InvoiceFilter | undefined>(undefined);

  // Estado público (readonly)
  readonly invoices = this._invoices.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly currentFilter = this._currentFilter.asReadonly();

  // Vistas computadas según tipo de pago
  readonly bankInvoices = computed(() =>
    this._invoices().filter(i => i.paymentType === PaymentType.Bank)
  );

  readonly cashInvoices = computed(() =>
    this._invoices().filter(i => i.paymentType === PaymentType.Cash)
  );

  // Facturas con baja confianza de extracción (requieren verificación)
  readonly lowConfidenceInvoices = computed(() =>
    this._invoices().filter(i => 
      i.extractionConfidence !== undefined && i.extractionConfidence < 0.7
    )
  );

  // Estadísticas
  readonly totalAmount = computed(() =>
    this._invoices().reduce((sum, inv) => sum + inv.totalAmount, 0)
  );

  readonly totalTaxAmount = computed(() =>
    this._invoices().reduce((sum, inv) => sum + (inv.taxAmount || 0), 0)
  );

  readonly totalSubtotalAmount = computed(() =>
    this._invoices().reduce((sum, inv) => sum + inv.subtotalAmount, 0)
  );

  readonly invoicesCount = computed(() => this._invoices().length);

  // Mantener compatibilidad con nombres antiguos (deprecated)
  readonly pendingInvoices = computed(() => []); // No aplica en modelo contable
  readonly reviewedInvoices = computed(() => []); // No aplica en modelo contable
  readonly errorInvoices = computed(() => this.lowConfidenceInvoices()); // Facturas con baja confianza
  readonly overdueInvoices = computed(() => []); // No aplica para InvoiceReceived
  readonly paidInvoices = computed(() => []); // No aplica para InvoiceReceived

  constructor(@Inject(INVOICE_API_PORT) private invoiceApi: InvoiceApiPort) {}

  /**
   * Carga todas las facturas con filtros opcionales.
   */
  async loadInvoices(filter?: InvoiceFilter): Promise<void> {
    this._loading.set(true);
    this._error.set(null);
    this._currentFilter.set(filter);

    try {
      const invoices = await this.invoiceApi.getAllInvoices(filter);
      this._invoices.set(invoices);
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al cargar facturas');
    } finally {
      this._loading.set(false);
    }
  }

  /**
   * Sube una nueva factura.
   */
  async uploadInvoice(file: File): Promise<Invoice> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const invoice = await this.invoiceApi.uploadInvoice(file);
      // Agregar a la lista actual (respetando filtros)
      const currentInvoices = this._invoices();
      this._invoices.set([invoice, ...currentInvoices]);
      return invoice;
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al subir factura');
      throw error;
    } finally {
      this._loading.set(false);
    }
  }

  /**
   * Actualiza una factura existente.
   */
  async updateInvoice(id: string, data: Partial<Invoice>): Promise<Invoice> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const updateRequest: any = {};
      if (data.invoiceNumber) updateRequest.invoiceNumber = data.invoiceNumber;
      if (data.supplierName) updateRequest.supplierName = data.supplierName;
      if (data.supplierTaxId !== undefined) updateRequest.supplierTaxId = data.supplierTaxId;
      if (data.issueDate) updateRequest.issueDate = data.issueDate.toISOString();
      if (data.receivedDate) updateRequest.receivedDate = data.receivedDate.toISOString();
      if (data.supplierAddress !== undefined) updateRequest.supplierAddress = data.supplierAddress;
      if (data.totalAmount !== undefined) updateRequest.totalAmount = data.totalAmount;
      if (data.taxAmount !== undefined) updateRequest.taxAmount = data.taxAmount;
      if (data.taxRate !== undefined) updateRequest.taxRate = data.taxRate;
      if (data.irpfAmount !== undefined) updateRequest.irpfAmount = data.irpfAmount;
      if (data.irpfRate !== undefined) updateRequest.irpfRate = data.irpfRate;
      if (data.subtotalAmount !== undefined) updateRequest.subtotalAmount = data.subtotalAmount;
      if (data.currency) updateRequest.currency = data.currency;
      if (data.notes !== undefined) updateRequest.notes = data.notes;
      if (data.lines) {
        updateRequest.lines = data.lines.map(line => ({
          id: line.id,
          lineNumber: line.lineNumber,
          description: line.description,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
          unitPriceCurrency: line.unitPriceCurrency || data.currency,
          taxRate: line.taxRate,
          lineTotal: line.lineTotal,
          lineTotalCurrency: line.lineTotalCurrency || data.currency
        }));
      }

      const updated = await this.invoiceApi.updateInvoice(id, updateRequest);
      
      // Actualizar en la lista
      const currentInvoices = this._invoices();
      const index = currentInvoices.findIndex(i => i.id === id);
      if (index >= 0) {
        currentInvoices[index] = updated;
        this._invoices.set([...currentInvoices]);
      }
      
      return updated;
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al actualizar factura');
      throw error;
    } finally {
      this._loading.set(false);
    }
  }

  /**
   * Elimina una factura.
   */
  async deleteInvoice(id: string): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      await this.invoiceApi.deleteInvoice(id);
      // Remover de la lista
      const currentInvoices = this._invoices();
      this._invoices.set(currentInvoices.filter(i => i.id !== id));
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al eliminar factura');
      throw error;
    } finally {
      this._loading.set(false);
    }
  }

  /**
   * Descarga el archivo original de una factura para visualización.
   */
  async downloadInvoice(id: string): Promise<Blob> {
    try {
      return await this.invoiceApi.downloadInvoice(id);
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al descargar factura');
      throw error;
    }
  }

  /**
   * Exporta las facturas actuales (con filtros aplicados) a Excel.
   */
  async exportToExcel(): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const filter = this._currentFilter();
      const blob = await this.invoiceApi.exportToExcel(filter);
      
      // Descargar el archivo
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `FacturasRecibidas_${new Date().toISOString().split('T')[0]}.xlsx`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al exportar a Excel');
      throw error;
    } finally {
      this._loading.set(false);
    }
  }

  /**
   * Envía una pregunta sobre las facturas al chat de OpenAI.
   */
  async chatAboutInvoices(question: string): Promise<{ answer: string; context?: any }> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const filter = this._currentFilter();
      const response = await this.invoiceApi.chatAboutInvoices(question, filter);
      return response;
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error en el chat');
      throw error;
    } finally {
      this._loading.set(false);
    }
  }
}
