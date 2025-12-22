import { Component, Input, Output, EventEmitter, OnInit, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { Invoice } from '../../../domain/invoice.model';

@Component({
  selector: 'app-invoice-edit-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    CalendarModule,
    InputTextareaModule
  ],
  templateUrl: './invoice-edit-dialog.component.html',
  styleUrls: ['./invoice-edit-dialog.component.css']
})
export class InvoiceEditDialogComponent implements OnInit, OnChanges {
  @Input() invoice: Invoice | null = null;
  @Input() visible: boolean = false;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() save = new EventEmitter<Partial<Invoice>>();

  editData: Partial<Invoice> = {};

  ngOnInit(): void {
    this.loadInvoiceData();
  }

  ngOnChanges(): void {
    this.loadInvoiceData();
  }

  private loadInvoiceData(): void {
    if (this.invoice) {
      this.editData = {
        invoiceNumber: this.invoice.invoiceNumber,
        supplierName: this.invoice.supplierName,
        supplierTaxId: this.invoice.supplierTaxId || '',
        supplierAddress: this.invoice.supplierAddress || '',
        issueDate: this.invoice.issueDate,
        receivedDate: this.invoice.receivedDate,
        totalAmount: this.invoice.totalAmount,
        taxAmount: this.invoice.taxAmount,
        taxRate: this.invoice.taxRate,
        subtotalAmount: this.invoice.subtotalAmount,
        currency: this.invoice.currency,
        notes: this.invoice.notes || ''
      };
    }
  }

  onSave(): void {
    this.save.emit(this.editData);
  }

  onCancel(): void {
    this.visibleChange.emit(false);
  }
}

