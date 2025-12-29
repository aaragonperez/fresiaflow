import { Invoice } from '../domain/invoice.model';

/**
 * Puerto (Port) para comunicación con la API de facturas.
 */
export interface UpdateInvoiceRequest {
  invoiceNumber?: string;
  supplierName?: string;
  supplierTaxId?: string;
  issueDate?: string;
  receivedDate?: string;
  dueDate?: string;
  supplierAddress?: string;
  totalAmount?: number;
  taxAmount?: number;
  taxRate?: number;
  subtotalAmount?: number;
  currency?: string;
  notes?: string;
  lines?: UpdateInvoiceLineRequest[];
}

export interface UpdateInvoiceLineRequest {
  id?: string;
  lineNumber: number;
  description: string;
  quantity: number;
  unitPrice: number;
  unitPriceCurrency?: string;
  taxRate?: number;
  lineTotal: number;
  lineTotalCurrency?: string;
}

export interface InvoiceFilter {
  year?: number;
  quarter?: number; // 1-4
  supplierName?: string;
  paymentType?: 'Bank' | 'Cash';
}

export interface InvoiceApiPort {
  getAllInvoices(filter?: InvoiceFilter): Promise<Invoice[]>;
  getInvoiceById(id: string): Promise<Invoice>;
  uploadInvoice(file: File): Promise<Invoice>;
  updateInvoice(id: string, data: UpdateInvoiceRequest): Promise<Invoice>;
  markAsPaid(invoiceId: string, transactionId: string): Promise<void>;
  deleteInvoice(id: string): Promise<void>;
  downloadInvoice(id: string): Promise<Blob>;
  exportToExcel(filter?: InvoiceFilter): Promise<Blob>;
  chatAboutInvoices(question: string, filter?: InvoiceFilter): Promise<{ answer: string; context?: any }>;
}

