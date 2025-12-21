import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InvoiceFacade } from '../../../application/invoice.facade';
import { InvoiceTableComponent } from '../../components/invoice-table/invoice-table.component';
import { InvoiceStatus } from '../../../domain/invoice.model';

/**
 * Componente de página para gestión de facturas.
 */
@Component({
  selector: 'app-invoices-page',
  standalone: true,
  imports: [CommonModule, InvoiceTableComponent],
  templateUrl: './invoices-page.component.html',
  styleUrls: ['./invoices-page.component.css']
})
export class InvoicesPageComponent implements OnInit {
  facade = inject(InvoiceFacade);
  
  invoices = this.facade.invoices;
  loading = this.facade.loading;
  error = this.facade.error;
  pendingInvoices = this.facade.pendingInvoices;
  overdueInvoices = this.facade.overdueInvoices;
  paidInvoices = this.facade.paidInvoices;

  InvoiceStatus = InvoiceStatus;

  ngOnInit(): void {
    this.facade.loadInvoices();
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      await this.facade.uploadInvoice(file);
    }
  }
}

