import { Invoice } from '../domain/invoice.model';

/**
 * Puerto (Port) para comunicación con la API de facturas.
 */
export interface InvoiceApiPort {
  getAllInvoices(): Promise<Invoice[]>;
  getInvoiceById(id: string): Promise<Invoice>;
  uploadInvoice(file: File): Promise<Invoice>;
  markAsPaid(invoiceId: string, transactionId: string): Promise<void>;
  deleteInvoice(id: string): Promise<void>;
}

