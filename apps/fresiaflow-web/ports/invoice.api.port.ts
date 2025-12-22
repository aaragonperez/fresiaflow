import { Invoice } from '../domain/invoice.model';

/**
 * Puerto (Port) para comunicación con la API de facturas.
 */
export interface UpdateInvoiceRequest {
  invoiceNumber?: string;
  supplierName?: string;
  supplierTaxId?: string;
  issueDate?: string;
  dueDate?: string;
  totalAmount?: number;
  taxAmount?: number;
  subtotalAmount?: number;
  currency?: string;
  notes?: string;
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
  exportToExcel(filter?: InvoiceFilter): Promise<Blob>;
  chatAboutInvoices(question: string, filter?: InvoiceFilter): Promise<{ answer: string; context?: any }>;
}

