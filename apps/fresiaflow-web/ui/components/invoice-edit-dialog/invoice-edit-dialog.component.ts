import { Component, Input, Output, EventEmitter, OnInit, OnChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { Table, TableModule } from 'primeng/table';
import { Invoice, InvoiceLine } from '../../../domain/invoice.model';

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
    InputTextareaModule,
    TableModule
  ],
  templateUrl: './invoice-edit-dialog.component.html',
  styleUrls: ['./invoice-edit-dialog.component.css']
})
export class InvoiceEditDialogComponent implements OnInit, OnChanges {
  @Input() invoice: Invoice | null = null;
  @Input() visible: boolean = false;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() save = new EventEmitter<Partial<Invoice>>();
  @ViewChild('linesTable') linesTable?: Table;

  editData: Partial<Invoice> = { lines: [] };
  readonly lineRows = 5;
  readonly lineRowsPerPageOptions = [5, 10, 20];

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
        irpfAmount: this.invoice.irpfAmount,
        irpfRate: this.invoice.irpfRate,
        subtotalAmount: this.invoice.subtotalAmount,
        currency: this.invoice.currency,
        notes: this.invoice.notes || '',
        lines: this.invoice.lines?.map(line => ({
          ...line
        })) || []
      };
    }
  }

  addLine(): void {
    const currency = this.editData.currency || 'EUR';
    const currentLines = this.editData.lines || [];
    const nextNumber = currentLines.reduce((max, line) => Math.max(max, line.lineNumber ?? 0), 0) + 1;
    const tempId = `tmp-${Date.now()}-${Math.floor(Math.random() * 10000)}`;

    const newLine: InvoiceLine = {
      id: tempId,
      lineNumber: nextNumber,
      description: '',
      quantity: 1,
      unitPrice: 0,
      unitPriceCurrency: currency,
      taxRate: this.editData.taxRate,
      lineTotal: 0,
      lineTotalCurrency: currency
    };

    this.editData = {
      ...this.editData,
      lines: [...currentLines, newLine]
    };
  }

  removeLine(index: number): void {
    const currentLines = [...(this.editData.lines || [])];
    currentLines.splice(index, 1);
    this.editData = {
      ...this.editData,
      lines: [...currentLines]
    };
  }

  resetLineFilters(): void {
    this.linesTable?.reset();
  }

  onSave(): void {
    this.save.emit(this.editData);
  }

  onCancel(): void {
    this.visibleChange.emit(false);
  }
}

