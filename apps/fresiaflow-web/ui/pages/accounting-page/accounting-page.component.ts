import { Component, OnInit, OnDestroy, signal, computed, ViewChild, inject, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule, Table } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { CheckboxModule } from 'primeng/checkbox';
import { ProgressBarModule } from 'primeng/progressbar';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';
import * as signalR from '@microsoft/signalr';
import { AccountingService, AccountingEntry, AccountingEntryLine, AccountingAccount, UpdateEntryRequest, FailedInvoice } from '../../../infrastructure/services/accounting.service';

@Component({
  selector: 'app-accounting-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    InputTextareaModule,
    DialogModule,
    DropdownModule,
    CalendarModule,
    InputNumberModule,
    TagModule,
    TooltipModule,
    CheckboxModule,
    ProgressBarModule,
    ProgressSpinnerModule,
    MessageModule
  ],
  templateUrl: './accounting-page.component.html',
  styleUrls: ['./accounting-page.component.css']
})
export class AccountingPageComponent implements OnInit, OnDestroy {
  @ViewChild('dt') table!: Table;

  service = inject(AccountingService);
  private http = inject(HttpClient);
  private ngZone = inject(NgZone);
  private hubConnection?: signalR.HubConnection;

  entries = this.service.entries;
  accounts = this.service.accounts;
  loading = this.service.loading;
  error = this.service.error;

  // Filtros
  filterStartDate = signal<Date | null>(null);
  filterEndDate = signal<Date | null>(null);
  filterStatus = signal<'Draft' | 'Posted' | 'Reversed' | null>(null);
  filterSource = signal<'Automatic' | 'Manual' | null>(null);
  filterErroneous = signal<boolean>(false);
  globalFilter = signal<string>('');
  
  // Selección múltiple
  selectedEntries = signal<AccountingEntry[]>([]);

  // Diálogo de edición
  editingEntry = signal<AccountingEntry | null>(null);
  editDialogVisible = signal<boolean>(false);
  editingLines = signal<AccountingEntryLine[]>([]);
  newLineAccount = signal<AccountingAccount | null>(null);
  newLineSide = signal<'Debit' | 'Credit'>('Debit');
  newLineAmount = signal<number>(0);
  newLineDescription = signal<string>('');

  // Estados
  generating = signal(false);
  regenerating = signal(false);
  postingBalanced = signal(false);
  expandedRowKeys = signal<{ [key: string]: boolean }>({});
  
  // Facturas fallidas
  failedInvoices = signal<FailedInvoice[]>([]);
  loadingFailedInvoices = signal(false);
  
  // Control de secciones colapsables
  entriesSectionExpanded = signal(true);
  failedInvoicesSectionExpanded = signal(true);
  
  // Progreso de generación
  generatingProgress = signal(0);
  generatingTotal = signal(0);
  generatingStatus = signal<'idle' | 'generating' | 'completed' | 'error' | 'cancelled'>('idle');
  generatingMessage = signal<string>('');
  generatingCurrentInvoice = signal<string | null>(null);
  generatingCurrentSupplier = signal<string | null>(null);
  generatingSuccessCount = signal(0);
  generatingErrorCount = signal(0);
  generatingCurrentError = signal<string | null>(null);
  
  // Computed para el porcentaje sin decimales
  generatingPercentage = computed(() => {
    if (this.generatingTotal() > 0) {
      return Math.round((this.generatingProgress() * 100) / this.generatingTotal());
    }
    return 0;
  });
  
  // Cache de nombres de cuentas para evitar búsquedas repetidas
  private accountNameCache = new Map<string, string>();

  // Estadísticas computadas
  totalEntries = computed(() => this.entries().length);
  draftEntries = computed(() => this.entries().filter(e => e.status === 'Draft').length);
  postedEntries = computed(() => this.entries().filter(e => e.status === 'Posted').length);
  erroneousEntries = computed(() => this.entries().filter(e => !e.isBalanced).length);
  statsTotalDebit = computed(() => this.entries().reduce((sum, e) => sum + e.totalDebit, 0));
  statsTotalCredit = computed(() => this.entries().reduce((sum, e) => sum + e.totalCredit, 0));
  balanceDifference = computed(() => Math.abs(this.statsTotalDebit() - this.statsTotalCredit()));
  
  // Estadísticas de facturas
  totalInvoices = computed(() => this.totalEntries() + this.failedInvoices().length);
  failedInvoicesCount = computed(() => this.failedInvoices().length);

  // Opciones
  statusOptions = [
    { label: 'Todos', value: null },
    { label: 'Borrador', value: 'Draft' },
    { label: 'Contabilizado', value: 'Posted' },
    { label: 'Anulado', value: 'Reversed' }
  ];

  sourceOptions = [
    { label: 'Todos', value: null },
    { label: 'Automático', value: 'Automatic' },
    { label: 'Manual', value: 'Manual' }
  ];

  sideOptions = [
    { label: 'Debe', value: 'Debit' },
    { label: 'Haber', value: 'Credit' }
  ];

  ngOnInit(): void {
    this.loadData();
    this.initializeSignalR();
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop();
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

    this.hubConnection.on('ReceiveAccountingProgress', (update: any) => {
      console.log('✅ SignalR - Progreso de contabilidad recibido:', update);
      console.log('   - ProcessedCount:', update.processedCount);
      console.log('   - TotalCount:', update.totalCount);
      console.log('   - Percentage:', update.percentage);
      console.log('   - Status:', update.status);
      console.log('   - Message:', update.message);
      
      this.ngZone.run(() => {
        this.generatingProgress.set(update.processedCount || 0);
        this.generatingTotal.set(update.totalCount || 0);
        this.generatingMessage.set(update.message || '');
        this.generatingCurrentInvoice.set(update.currentInvoiceNumber || null);
        this.generatingCurrentSupplier.set(update.currentInvoiceSupplier || null);
        this.generatingSuccessCount.set(update.successCount || 0);
        this.generatingErrorCount.set(update.errorCount || 0);
        this.generatingCurrentError.set(update.currentError || null);
        
        // Asegurar que el estado sea 'generating' si está procesando
        if (update.status === 'generating' && this.generatingStatus() !== 'generating') {
          this.generatingStatus.set('generating');
        }
        
        if (update.status === 'completed') {
          this.generatingStatus.set('completed');
          this.generating.set(false);
          this.regenerating.set(false);
          // Recargar datos después de un breve delay
          setTimeout(() => {
            this.loadData();
            // Limpiar estado después de mostrar el mensaje
            setTimeout(() => {
              this.generatingStatus.set('idle');
              this.generatingMessage.set('');
              this.generatingCurrentInvoice.set(null);
              this.generatingCurrentSupplier.set(null);
              this.generatingCurrentError.set(null);
            }, 3000);
          }, 1000);
        } else if (update.status === 'error') {
          this.generatingStatus.set('error');
          this.generating.set(false);
          this.regenerating.set(false);
        } else if (update.status === 'generating') {
          this.generatingStatus.set('generating');
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
        console.log('✅ SignalR conectado correctamente para contabilidad. ConnectionId:', this.hubConnection?.connectionId);
      })
      .catch(err => {
        console.error('❌ Error conectando SignalR:', err);
      });
  }

  async loadData(): Promise<void> {
    await Promise.all([
      this.service.loadEntries(this.getFilters()),
      this.service.loadAccounts(),
      this.loadFailedInvoices()
    ]);
    // Limpiar cache cuando se cargan nuevas cuentas
    this.accountNameCache.clear();
  }

  async loadFailedInvoices(): Promise<void> {
    this.loadingFailedInvoices.set(true);
    try {
      const failed = await this.service.getFailedInvoices();
      this.failedInvoices.set(failed);
    } catch (err: any) {
      console.error('Error cargando facturas fallidas:', err);
      this.failedInvoices.set([]);
    } finally {
      this.loadingFailedInvoices.set(false);
    }
  }

  async viewInvoice(invoiceId: string): Promise<void> {
    try {
      const blob = await firstValueFrom(
        this.http.get(`/api/invoices/${invoiceId}/download`, {
          responseType: 'blob'
        })
      );
      
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 100);
    } catch (err: any) {
      alert('Error al abrir el archivo: ' + (err.message || 'Error desconocido'));
    }
  }

  getFilters(): any {
    return {
      startDate: this.filterStartDate()?.toISOString(),
      endDate: this.filterEndDate()?.toISOString(),
      status: this.filterStatus() || undefined,
      source: this.filterSource() || undefined
    };
  }

  async onFilterChange(): Promise<void> {
    // Los filtros por columna se manejan directamente en el template
    // Este método se mantiene para compatibilidad con filtros del caption si se necesitan
    await this.service.loadEntries(this.getFilters());
  }

  onFilterClear(): void {
    this.filterStartDate.set(null);
    this.filterEndDate.set(null);
    this.filterStatus.set(null);
    this.filterSource.set(null);
    this.filterErroneous.set(false);
    this.globalFilter.set('');
    if (this.table) {
      this.table.reset();
    }
    this.loadData();
  }

  async generateEntries(): Promise<void> {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'accounting-page.component.ts:268',message:'generateEntries called',data:{},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'F'})}).catch(()=>{});
    // #endregion
    
    this.generating.set(true);
    this.generatingStatus.set('generating');
    this.generatingProgress.set(0);
    this.generatingTotal.set(0);
    this.generatingMessage.set('Iniciando generación de asientos...');
    this.generatingSuccessCount.set(0);
    this.generatingErrorCount.set(0);
    this.generatingCurrentInvoice.set(null);
    this.generatingCurrentSupplier.set(null);
    this.generatingCurrentError.set(null);
    
    try {
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'accounting-page.component.ts:281',message:'Calling service.generateEntries',data:{},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'F'})}).catch(()=>{});
      // #endregion
      
      const result = await this.service.generateEntries();
      
      // #region agent log
      fetch('http://127.0.0.1:7242/ingest/49c267d0-fd94-47bb-be0e-0d247024240a',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'accounting-page.component.ts:283',message:'service.generateEntries completed',data:{totalProcessed:result.totalProcessed,successCount:result.successCount,errorCount:result.errorCount},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'F'})}).catch(()=>{});
      // #endregion
      // Actualizar facturas fallidas
      if (result.failedInvoices && result.failedInvoices.length > 0) {
        this.failedInvoices.set(result.failedInvoices);
      } else {
        // Si no vienen en el resultado, cargarlas manualmente
        await this.loadFailedInvoices();
      }
      
      // El estado se actualizará automáticamente a través de SignalR
      // Solo mostrar alert si SignalR no actualizó el estado
      if (this.generatingStatus() === 'generating') {
        this.generatingStatus.set('completed');
        this.generatingMessage.set(`Generados ${result.successCount} asientos. ${result.errorCount > 0 ? `Errores: ${result.errorCount}` : ''}`);
        setTimeout(() => {
          this.generatingStatus.set('idle');
          this.generating.set(false);
        }, 3000);
      }
      await this.loadData();
    } catch (err: any) {
      this.generatingStatus.set('error');
      this.generatingMessage.set('Error: ' + (err.error?.error || err.message || 'Error desconocido'));
      this.generating.set(false);
      setTimeout(() => {
        this.generatingStatus.set('idle');
      }, 3000);
    }
  }

  async cancelGeneration(): Promise<void> {
    if (!confirm('¿Estás seguro de cancelar la generación en progreso?')) {
      return;
    }

    try {
      await this.service.cancelGeneration();
      this.generatingStatus.set('cancelled');
      this.generatingMessage.set('Generación cancelada por el usuario');
      this.regenerating.set(false);
      setTimeout(() => {
        this.generatingStatus.set('idle');
      }, 3000);
    } catch (err: any) {
      alert('Error cancelando generación: ' + (err.error?.error || err.message || 'Error desconocido'));
    }
  }

  async regenerateAllEntries(): Promise<void> {
    if (!confirm('¿Estás seguro de regenerar TODOS los asientos? Esto eliminará los asientos automáticos en borrador y creará nuevos.')) {
      return;
    }

    this.regenerating.set(true);
    this.generatingStatus.set('generating');
    this.generatingProgress.set(0);
    this.generatingTotal.set(0);
    this.generatingMessage.set('Iniciando regeneración de asientos...');
    this.generatingSuccessCount.set(0);
    this.generatingErrorCount.set(0);
    this.generatingCurrentInvoice.set(null);
    this.generatingCurrentSupplier.set(null);
    this.generatingCurrentError.set(null);
    
    try {
      const result = await this.service.regenerateAllEntries();
      
      // Actualizar facturas fallidas
      await this.loadFailedInvoices();
      
      // El estado se actualizará automáticamente a través de SignalR
      // Solo mostrar alert si SignalR no actualizó el estado
      if (this.generatingStatus() === 'generating') {
        this.generatingStatus.set('completed');
        this.generatingMessage.set(`Regenerados ${result.successCount} asientos. ${result.errorCount > 0 ? `Errores: ${result.errorCount}` : ''}`);
        setTimeout(() => {
          this.generatingStatus.set('idle');
          this.regenerating.set(false);
        }, 3000);
      }
      this.selectedEntries.set([]);
      await this.loadData();
    } catch (err: any) {
      this.generatingStatus.set('error');
      this.generatingMessage.set('Error: ' + (err.error?.error || err.message || 'Error desconocido'));
      this.regenerating.set(false);
      setTimeout(() => {
        this.generatingStatus.set('idle');
      }, 3000);
    }
  }

  async regenerateSelectedEntries(): Promise<void> {
    const selected = this.selectedEntries();
    if (selected.length === 0) {
      alert('Selecciona al menos un asiento para regenerar.');
      return;
    }

    if (!confirm(`¿Estás seguro de regenerar ${selected.length} asiento(s)? Esto eliminará los asientos seleccionados y creará nuevos.`)) {
      return;
    }

    this.regenerating.set(true);
    this.generatingStatus.set('generating');
    this.generatingProgress.set(0);
    this.generatingTotal.set(0);
    this.generatingMessage.set(`Iniciando regeneración de ${selected.length} asiento(s)...`);
    this.generatingSuccessCount.set(0);
    this.generatingErrorCount.set(0);
    this.generatingCurrentInvoice.set(null);
    this.generatingCurrentSupplier.set(null);
    this.generatingCurrentError.set(null);
    
    try {
      const entryIds = selected.map(e => e.id);
      const result = await this.service.regenerateSelectedEntries(entryIds);
      
      // Actualizar facturas fallidas
      await this.loadFailedInvoices();
      
      // El estado se actualizará automáticamente a través de SignalR
      // Solo mostrar alert si SignalR no actualizó el estado
      if (this.generatingStatus() === 'generating') {
        this.generatingStatus.set('completed');
        this.generatingMessage.set(`Regenerados ${result.successCount} asientos. ${result.errorCount > 0 ? `Errores: ${result.errorCount}` : ''}`);
        setTimeout(() => {
          this.generatingStatus.set('idle');
          this.regenerating.set(false);
        }, 3000);
      }
      this.selectedEntries.set([]);
      await this.loadData();
    } catch (err: any) {
      this.generatingStatus.set('error');
      this.generatingMessage.set('Error: ' + (err.error?.error || err.message || 'Error desconocido'));
      this.regenerating.set(false);
      setTimeout(() => {
        this.generatingStatus.set('idle');
      }, 3000);
    }
  }

  onSelectionChange(entries: AccountingEntry[]): void {
    this.selectedEntries.set(entries);
  }

  // Filtro para asientos erróneos (no balanceados)
  filteredEntries = computed(() => {
    const entries = this.entries();
    if (!this.filterErroneous()) {
      return entries;
    }
    return entries.filter(e => !e.isBalanced);
  });

  async postEntry(entry: AccountingEntry): Promise<void> {
    if (!confirm('¿Contabilizar este asiento? No se podrá modificar después.')) {
      return;
    }

    try {
      await this.service.postEntry(entry.id);
      await this.loadData();
    } catch (err: any) {
      alert('Error: ' + (err.error?.error || err.message || 'Error desconocido'));
    }
  }

  async postAllBalancedEntries(): Promise<void> {
    const balancedDraftEntries = this.entries().filter(e => e.status === 'Draft' && e.isBalanced);
    if (balancedDraftEntries.length === 0) {
      alert('No hay asientos balanceados en borrador para contabilizar.');
      return;
    }

    if (!confirm(`¿Contabilizar ${balancedDraftEntries.length} asiento(s) balanceado(s)? No se podrán modificar después.`)) {
      return;
    }

    this.postingBalanced.set(true);
    try {
      const result = await this.service.postAllBalancedEntries();
      const message = `Contabilizados ${result.successCount} asientos. ${result.errorCount > 0 ? `Errores: ${result.errorCount}` : ''}`;
      alert(message);
      await this.loadData();
    } catch (err: any) {
      alert('Error: ' + (err.error?.error || err.message || 'Error desconocido'));
    } finally {
      this.postingBalanced.set(false);
    }
  }

  openEditDialog(entry: AccountingEntry): void {
    if (entry.status === 'Posted' && entry.source === 'Automatic') {
      alert('No se pueden modificar asientos automáticos ya contabilizados.');
      return;
    }

    // Crear una copia del entry para edición
    const entryCopy = { ...entry };
    this.editingEntry.set(entryCopy);
    this.editingLines.set(entry.lines.map(l => ({ ...l })));
    this.editDialogVisible.set(true);
  }

  closeEditDialog(): void {
    this.editDialogVisible.set(false);
    this.editingEntry.set(null);
    this.editingLines.set([]);
  }

  onDialogVisibleChange(visible: boolean): void {
    this.editDialogVisible.set(visible);
    if (!visible) {
      this.closeEditDialog();
    }
  }

  addNewLine(): void {
    if (!this.newLineAccount() || this.newLineAmount() <= 0) {
      alert('Selecciona una cuenta y un importe mayor que cero.');
      return;
    }

    const newLine: AccountingEntryLine = {
      id: crypto.randomUUID(),
      accountingAccountId: this.newLineAccount()!.id,
      side: this.newLineSide(),
      amount: this.newLineAmount(),
      currency: 'EUR',
      description: this.newLineDescription() || undefined
    };

    this.editingLines.set([...this.editingLines(), newLine]);

    // Reset form
    this.newLineAccount.set(null);
    this.newLineAmount.set(0);
    this.newLineDescription.set('');
  }

  removeLine(lineId: string): void {
    this.editingLines.set(this.editingLines().filter(l => l.id !== lineId));
  }

  async saveEntry(): Promise<void> {
    const entry = this.editingEntry();
    if (!entry) return;

    // Validar que esté balanceado
    const totalDebit = this.editingLines()
      .filter(l => l.side === 'Debit')
      .reduce((sum, l) => sum + l.amount, 0);
    
    const totalCredit = this.editingLines()
      .filter(l => l.side === 'Credit')
      .reduce((sum, l) => sum + l.amount, 0);

    if (Math.abs(totalDebit - totalCredit) > 0.01) {
      alert(`El asiento no está balanceado. Debe: ${totalDebit.toFixed(2)}, Haber: ${totalCredit.toFixed(2)}`);
      return;
    }

    try {
      const request: UpdateEntryRequest = {
        description: entry.description,
        entryDate: entry.entryDate,
        notes: entry.notes,
        // Enviar líneas incluyendo el id si existe (para preservar líneas existentes)
        // Convertir side de string a número (1=Debit, 2=Credit)
        lines: this.editingLines().map(l => ({
          id: l.id || undefined, // Incluir ID si existe
          accountingAccountId: l.accountingAccountId,
          side: l.side === 'Debit' ? 1 : 2, // Convertir a número del enum
          amount: l.amount,
          currency: l.currency || 'EUR',
          description: l.description
        })) as any
      };

      await this.service.updateEntry(entry.id, request);
      this.closeEditDialog();
      await this.loadData();
    } catch (err: any) {
      alert('Error: ' + (err.error?.error || err.message || 'Error desconocido'));
    }
  }

  getAccountName(accountId: string): string {
    return this.getAccountNameCached(accountId);
  }

  getStatusSeverity(status: string): string {
    switch (status) {
      case 'Posted': return 'success';
      case 'Draft': return 'warning';
      case 'Reversed': return 'danger';
      default: return 'info';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Posted': return 'Contabilizado';
      case 'Draft': return 'Borrador';
      case 'Reversed': return 'Anulado';
      default: return status;
    }
  }

  getSourceLabel(source: string): string {
    return source === 'Automatic' ? 'Automático' : 'Manual';
  }

  onRowExpand(event: any): void {
    const entry = event.data;
    if (!entry || !entry.id) return;
    const keys = { ...this.expandedRowKeys() };
    keys[entry.id] = true;
    this.expandedRowKeys.set(keys);
  }

  onRowCollapse(event: any): void {
    const entry = event.data;
    if (!entry || !entry.id) return;
    const keys = { ...this.expandedRowKeys() };
    delete keys[entry.id];
    this.expandedRowKeys.set(keys);
  }
  
  toggleRow(entry: AccountingEntry): void {
    const keys = { ...this.expandedRowKeys() };
    if (keys[entry.id]) {
      delete keys[entry.id];
    } else {
      keys[entry.id] = true;
    }
    this.expandedRowKeys.set(keys);
  }

  // Computed signals para el diálogo de edición (optimización)
  editingEntryDate = computed(() => {
    const entry = this.editingEntry();
    if (!entry || !entry.entryDate) return null;
    return new Date(entry.entryDate);
  });

  editingEntryDescription = computed(() => {
    return this.editingEntry()?.description || '';
  });

  editingEntryNotes = computed(() => {
    return this.editingEntry()?.notes || '';
  });

  editingEntryHeader = computed(() => {
    const entry = this.editingEntry();
    return entry ? `Editar Asiento: ${entry.description}` : 'Editar Asiento';
  });

  isEditingEntryReadonly = computed(() => {
    const entry = this.editingEntry();
    return entry ? (entry.status === 'Posted' && entry.source === 'Automatic') : false;
  });

  totalDebit = computed(() => {
    const lines = this.editingLines();
    return lines
      .filter(l => l.side === 'Debit')
      .reduce((sum, l) => sum + l.amount, 0);
  });

  totalCredit = computed(() => {
    const lines = this.editingLines();
    return lines
      .filter(l => l.side === 'Credit')
      .reduce((sum, l) => sum + l.amount, 0);
  });

  // Métodos para actualizar valores
  setEditingEntryDate(date: Date | null): void {
    const entry = this.editingEntry();
    if (entry) {
      entry.entryDate = date ? date.toISOString() : '';
    }
  }

  setEditingEntryDescription(value: string): void {
    const entry = this.editingEntry();
    if (entry) {
      entry.description = value;
    }
  }

  setEditingEntryNotes(value: string): void {
    const entry = this.editingEntry();
    if (entry) {
      entry.notes = value;
    }
  }

  // Método optimizado para obtener nombre de cuenta con cache
  getAccountNameCached(accountId: string): string {
    if (!accountId) return '';
    
    if (this.accountNameCache.has(accountId)) {
      return this.accountNameCache.get(accountId)!;
    }
    
    const accounts = this.accounts();
    if (!accounts || accounts.length === 0) {
      return accountId;
    }
    
    const account = accounts.find(a => a.id === accountId);
    const name = account ? `${account.code} - ${account.name}` : accountId;
    this.accountNameCache.set(accountId, name);
    return name;
  }

  // TrackBy para optimizar el renderizado de líneas
  trackByLineId(index: number, line: AccountingEntryLine): string {
    return line.id || `line-${index}`;
  }
}
