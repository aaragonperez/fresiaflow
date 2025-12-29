import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { InvoiceFacade } from '../../../application/invoice.facade';
import { InvoiceTableComponent } from '../../components/invoice-table/invoice-table.component';
import { InvoiceEditDialogComponent } from '../../components/invoice-edit-dialog/invoice-edit-dialog.component';
import { Invoice, PaymentType } from '../../../domain/invoice.model';
import { InvoiceFilter } from '../../../ports/invoice.api.port';
import { ChatActionService } from '../../../application/chat-action.service';

/**
 * Componente de página para gestión de facturas recibidas.
 * Refleja el modelo contable: todas las facturas están contabilizadas desde su recepción.
 */
@Component({
  selector: 'app-invoices-page',
  standalone: true,
  imports: [CommonModule, FormsModule, InvoiceTableComponent, InvoiceEditDialogComponent],
  templateUrl: './invoices-page.component.html',
  styleUrls: ['./invoices-page.component.css']
})
export class InvoicesPageComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private actionService = inject(ChatActionService);
  facade = inject(InvoiceFacade);
  
  private actionSubscription?: Subscription;
  
  invoices = this.facade.invoices;
  loading = this.facade.loading;
  error = this.facade.error;
  bankInvoices = this.facade.bankInvoices;
  cashInvoices = this.facade.cashInvoices;
  lowConfidenceInvoices = this.facade.lowConfidenceInvoices;
  totalAmount = this.facade.totalAmount;
  totalTaxAmount = this.facade.totalTaxAmount;
  totalSubtotalAmount = this.facade.totalSubtotalAmount;

  // Filtros contables
  filterYear = signal<number | null>(null);
  filterQuarter = signal<number | null>(null);
  filterSupplier = signal<string>('');
  filterPaymentType = signal<string | null>(null);

  // Estados de colapso de secciones
  filtersCollapsed = signal<boolean>(false);
  uploadCollapsed = signal<boolean>(false);
  tableCollapsed = signal<boolean>(false);

  isDragging = false;
  PaymentType = PaymentType;

  // Diálogo de edición
  editingInvoice = signal<Invoice | null>(null);
  editDialogVisible = signal<boolean>(false);

  // Años disponibles (desde 2014 hasta el año actual)
  readonly availableYears = Array.from(
    { length: new Date().getFullYear() - 2014 + 1 }, 
    (_, i) => 2014 + i
  ).reverse();
  readonly quarters = [1, 2, 3, 4];

  ngOnInit(): void {
    this.loadInvoices().then(() => {
      // Verificar si hay un parámetro para editar una factura
      this.route.queryParams.subscribe(params => {
        const editInvoiceId = params['editInvoiceId'];
        if (editInvoiceId) {
          this.openInvoiceForEdit(editInvoiceId);
          // Limpiar el parámetro de la URL para evitar que se reabra
          this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true
          });
        }
      });
    });

    // Suscribirse a acciones del chat
    this.actionSubscription = this.actionService.action$.subscribe(action => {
      this.executeChatAction(action);
    });
  }

  ngOnDestroy(): void {
    this.actionSubscription?.unsubscribe();
  }

  /**
   * Abre una factura para edición dado su ID.
   */
  private openInvoiceForEdit(invoiceId: string): void {
    const invoice = this.invoices().find(inv => inv.id === invoiceId);
    if (invoice) {
      this.editingInvoice.set(invoice);
      this.editDialogVisible.set(true);
    } else {
      // Si no se encuentra, intentar buscar por número de factura
      const invoiceByNumber = this.invoices().find(inv => inv.invoiceNumber === invoiceId);
      if (invoiceByNumber) {
        this.editingInvoice.set(invoiceByNumber);
        this.editDialogVisible.set(true);
      }
    }
  }

  /**
   * Carga facturas con los filtros actuales.
   */
  async loadInvoices(): Promise<void> {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:59',message:'loadInvoices entry',data:{signals:{year:this.filterYear(),quarter:this.filterQuarter(),supplier:this.filterSupplier(),paymentType:this.filterPaymentType()}},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'E'})}).catch(()=>{});
    // #endregion
    const filter: InvoiceFilter = {};
    
    const year = this.filterYear();
    const quarter = this.filterQuarter();
    const supplier = this.filterSupplier().trim();
    const paymentType = this.filterPaymentType();
    
    console.log('loadInvoices - Filtros actuales:', { year, quarter, supplier, paymentType });
    
    // Año
    if (year) {
      filter.year = year;
    }
    
    // Trimestre (puede enviarse incluso sin año, el backend usará el año actual)
    if (quarter !== null && quarter !== undefined && quarter >= 1 && quarter <= 4) {
      filter.quarter = Number(quarter);
    }
    
    // Proveedor
    if (supplier) {
      filter.supplierName = supplier;
    }
    
    // Tipo de pago
    if (paymentType && (paymentType === 'Bank' || paymentType === 'Cash')) {
      filter.paymentType = paymentType as 'Bank' | 'Cash';
    }

    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:90',message:'loadInvoices filter built',data:{filter,filterKeys:Object.keys(filter)},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'B'})}).catch(()=>{});
    // #endregion
    console.log('loadInvoices - Filtro final:', filter);
    await this.facade.loadInvoices(Object.keys(filter).length > 0 ? filter : undefined);
  }

  /**
   * Maneja el cambio de año.
   */
  onYearChange(value: number | null): void {
    this.filterYear.set(value);
    // Si se limpia el año, también limpiar el trimestre
    if (!value) {
      this.filterQuarter.set(null);
    }
    this.loadInvoices();
  }

  /**
   * Maneja el cambio de trimestre.
   */
  onQuarterChange(value: number | null): void {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:108',message:'onQuarterChange entry',data:{rawValue:value,valueType:typeof value},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
    // #endregion
    console.log('onQuarterChange called with:', value, 'type:', typeof value);
    // Asegurar que el valor sea un número o null
    const quarter = value === null || value === undefined ? null : Number(value);
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:112',message:'onQuarterChange processed',data:{processedQuarter:quarter,previousQuarter:this.filterQuarter()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
    // #endregion
    this.filterQuarter.set(quarter);
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:115',message:'onQuarterChange signal updated',data:{currentQuarter:this.filterQuarter()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'E'})}).catch(()=>{});
    // #endregion
    this.loadInvoices();
  }

  /**
   * Maneja el cambio de tipo de pago.
   */
  onPaymentTypeChange(value: string | null): void {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:119',message:'onPaymentTypeChange entry',data:{rawValue:value,valueType:typeof value},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
    // #endregion
    console.log('onPaymentTypeChange called with:', value, 'type:', typeof value);
    // Convertir string vacío a null
    const paymentType = value === '' || value === 'null' ? null : value;
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:123',message:'onPaymentTypeChange processed',data:{processedPaymentType:paymentType,previousPaymentType:this.filterPaymentType()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
    // #endregion
    this.filterPaymentType.set(paymentType);
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'invoices-page.component.ts:125',message:'onPaymentTypeChange signal updated',data:{currentPaymentType:this.filterPaymentType()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'E'})}).catch(()=>{});
    // #endregion
    this.loadInvoices();
  }

  /**
   * Limpia todos los filtros.
   */
  clearFilters(): void {
    this.filterYear.set(null);
    this.filterQuarter.set(null);
    this.filterSupplier.set('');
    this.filterPaymentType.set(null);
    this.loadInvoices();
  }

  /**
   * Exporta las facturas actuales a Excel.
   */
  async exportToExcel(): Promise<void> {
    try {
      await this.facade.exportToExcel();
    } catch (error) {
      console.error('Error al exportar:', error);
      alert('Error al exportar a Excel. Por favor, inténtalo de nuevo.');
    }
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const files = Array.from(input.files);
      await this.handleMultipleFileUpload(files);
      input.value = '';
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  async onDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      const fileArray = Array.from(files);
      const validExtensions = ['.pdf', '.jpg', '.jpeg', '.png', '.gif', '.webp'];
      const validTypes = ['application/pdf', 'image/jpeg', 'image/png', 'image/gif', 'image/webp'];
      
      const validFiles = fileArray.filter(file => {
        const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
        return validExtensions.includes(fileExtension) || validTypes.includes(file.type);
      });

      if (validFiles.length === 0) {
        alert('Por favor, sube solo archivos PDF o imágenes (JPG, PNG, GIF, WEBP).');
        return;
      }

      if (validFiles.length < fileArray.length) {
        alert(`Se filtraron ${fileArray.length - validFiles.length} archivos inválidos.`);
      }

      await this.handleMultipleFileUpload(validFiles);
    }
  }

  private async handleMultipleFileUpload(files: File[]): Promise<void> {
    const validFiles: File[] = [];
    const invalidFileNames: string[] = [];
    const validExtensions = ['.pdf', '.jpg', '.jpeg', '.png', '.gif', '.webp'];
    const validTypes = ['application/pdf', 'image/jpeg', 'image/png', 'image/gif', 'image/webp'];

    for (const file of files) {
      const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
      if (validExtensions.includes(fileExtension) || validTypes.includes(file.type)) {
        validFiles.push(file);
      } else {
        invalidFileNames.push(file.name);
      }
    }

    if (invalidFileNames.length > 0) {
      alert(`Los siguientes archivos no son válidos y no se subirán: ${invalidFileNames.join(', ')}. Por favor, sube solo archivos PDF o imágenes (JPG, PNG, GIF, WEBP).`);
    }

    if (validFiles.length === 0) {
      return;
    }

    // Procesar archivos uno a uno para capturar errores individuales
    const errors: string[] = [];
    let successCount = 0;

    for (const file of validFiles) {
      try {
        await this.facade.uploadInvoice(file);
        successCount++;
      } catch (error: any) {
        console.error(`Error al subir ${file.name}:`, error);
        const errorMessage = error?.message || `Error desconocido al procesar "${file.name}"`;
        errors.push(errorMessage);
      }
    }

    // Recargar facturas para reflejar los nuevos datos
    await this.loadInvoices();

    // Mostrar resumen de resultados
    if (errors.length > 0) {
      const summary = successCount > 0 
        ? `Se subieron ${successCount} factura(s) correctamente.\n\n`
        : '';
      const errorDetails = errors.length === 1
        ? `Error:\n${errors[0]}`
        : `Se encontraron ${errors.length} errores:\n\n${errors.map((e, i) => `${i + 1}. ${e}`).join('\n\n')}`;
      
      alert(`${summary}${errorDetails}`);
    }

    (document.getElementById('fileInput') as HTMLInputElement).value = '';
  }

  async onViewInvoice(invoice: Invoice): Promise<void> {
    try {
      const blob = await this.facade.downloadInvoice(invoice.id);
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 100);
    } catch (err: any) {
      alert('Error al abrir el archivo: ' + (err.message || 'Error desconocido'));
    }
  }

  async onDeleteInvoice(id: string): Promise<void> {
    if (confirm('¿Estás seguro de que quieres eliminar esta factura?')) {
      try {
        await this.facade.deleteInvoice(id);
      } catch (error) {
        console.error('Error al eliminar factura:', error);
        alert('Error al eliminar la factura. Por favor, inténtalo de nuevo.');
      }
    }
  }

  async onEditInvoice(invoice: Invoice): Promise<void> {
    this.editingInvoice.set(invoice);
    this.editDialogVisible.set(true);
  }

  async onSaveInvoice(updatedData: Partial<Invoice>): Promise<void> {
    const invoice = this.editingInvoice();
    if (!invoice) return;

    try {
      await this.facade.updateInvoice(invoice.id, updatedData);
      this.editDialogVisible.set(false);
      this.editingInvoice.set(null);
      // Recargar facturas para reflejar los cambios
      await this.loadInvoices();
    } catch (error: any) {
      console.error('Error al actualizar factura:', error);
      const errorMessage = error?.error?.error || error?.message || 'Error desconocido al actualizar la factura';
      alert(`Error al actualizar la factura: ${errorMessage}`);
    }
  }

  onCancelEdit(): void {
    this.editDialogVisible.set(false);
    this.editingInvoice.set(null);
  }

  /**
   * Ejecuta una acción recibida del chat IA.
   */
  executeChatAction(action: { type: string; params: Record<string, any> }): void {
    console.log('Ejecutando acción del chat:', action);

    switch (action.type) {
      case 'applyInvoiceFilter':
        // Aplicar filtros de facturas
        const params = action.params;
        
        if (params.year !== undefined && params.year !== null) {
          this.filterYear.set(Number(params.year));
        }
        
        if (params.quarter !== undefined && params.quarter !== null) {
          this.filterQuarter.set(Number(params.quarter));
        }
        
        if (params.supplierName !== undefined && params.supplierName !== null) {
          this.filterSupplier.set(String(params.supplierName));
        }
        
        if (params.paymentType !== undefined && params.paymentType !== null) {
          this.filterPaymentType.set(String(params.paymentType));
        }
        
        // Cargar facturas con los nuevos filtros
        this.loadInvoices();
        break;

      case 'navigateToPage':
        // Navegar a una página específica
        if (action.params.route) {
          this.router.navigate([action.params.route]);
        }
        break;

      case 'openInvoice':
        // Abrir una factura específica
        if (action.params.invoiceId) {
          this.openInvoiceForEdit(String(action.params.invoiceId));
        }
        break;

      default:
        console.warn('Acción desconocida del chat:', action.type);
    }
  }
}
