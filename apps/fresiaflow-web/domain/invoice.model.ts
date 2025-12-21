/**
 * Modelo de dominio para facturas.
 * Representa la entidad Invoice del backend.
 */
export interface Invoice {
  id: string;
  invoiceNumber: string;
  issueDate: Date;
  dueDate?: Date;
  amount: Money;
  status: InvoiceStatus;
  supplierName: string;
  filePath?: string;
  createdAt: Date;
  reconciledAt?: Date;
  reconciledWithTransactionId?: string;
}

export interface Money {
  value: number;
  currency: string;
}

export enum InvoiceStatus {
  Pending = 0,
  Paid = 1,
  Overdue = 2,
  Cancelled = 3
}

