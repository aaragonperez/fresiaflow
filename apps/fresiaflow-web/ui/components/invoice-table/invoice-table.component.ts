import { Component, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule, Table } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { RippleModule } from 'primeng/ripple';
import { Invoice } from '../../../domain/invoice.model';

/**
 * Componente presentacional para tabla de facturas recibidas.
 * Muestra todos los datos fiscales, económicos y de detalle.
 */
@Component({
  selector: 'app-invoice-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    TooltipModule,
    InputTextModule,
    InputNumberModule,
    DropdownModule,
    CalendarModule,
    RippleModule
  ],
  templateUrl: './invoice-table.component.html',
  styleUrls: ['./invoice-table.component.css']
})
export class InvoiceTableComponent {
  @ViewChild('dt') table!: Table;
  
  @Input() invoices: Invoice[] = [];
  @Output() deleteInvoice = new EventEmitter<string>();
  @Output() editInvoice = new EventEmitter<Invoice>();
  @Output() viewInvoice = new EventEmitter<Invoice>();

  readonly rows = 10;
  readonly rowsPerPageOptions = [10, 25, 50];
  expandedRowKeys: Record<string, boolean> = {};

  onRowExpand(invoiceId?: string): void {
    if (!invoiceId) return;
    this.expandedRowKeys = { ...this.expandedRowKeys, [invoiceId]: true };
  }

  onRowCollapse(invoiceId?: string): void {
    if (!invoiceId) return;
    const { [invoiceId]: _, ...rest } = this.expandedRowKeys;
    this.expandedRowKeys = rest;
  }

  onView(invoice: Invoice): void {
    this.viewInvoice.emit(invoice);
  }

  onEdit(invoice: Invoice): void {
    this.editInvoice.emit(invoice);
  }

  onDelete(invoiceId: string): void {
    if (confirm('¿Estás seguro de que quieres eliminar esta factura?')) {
      this.deleteInvoice.emit(invoiceId);
    }
  }
}

