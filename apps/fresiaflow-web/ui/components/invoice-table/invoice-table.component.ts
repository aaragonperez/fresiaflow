import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Invoice } from '../../../domain/invoice.model';

/**
 * Componente presentacional para tabla de facturas.
 */
@Component({
  selector: 'app-invoice-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './invoice-table.component.html',
  styleUrls: ['./invoice-table.component.css']
})
export class InvoiceTableComponent {
  @Input() invoices: Invoice[] = [];
}

