/**
 * Modelo de dominio para facturas recibidas.
 * Refleja el modelo contable real: todas las facturas están contabilizadas desde su recepción.
 * No existen estados ficticios de "pendiente" o "en proceso".
 */
export interface Invoice {
  id: string;
  invoiceNumber: string;
  issueDate: Date;
  receivedDate: Date; // Fecha de recepción (contable)
  
  // Proveedor
  supplierName: string;
  supplierTaxId?: string;
  supplierAddress?: string;
  
  // Importes
  subtotalAmount: number; // Base imponible
  taxAmount?: number; // IVA
  taxRate?: number; // Tipo de IVA (21%, 10%, etc.)
  totalAmount: number; // Total factura
  currency: string;
  
  // Pago
  paymentType: PaymentType; // "Bank" o "Cash"
  payments: InvoicePayment[]; // Pagos bancarios asociados
  
  // Metadatos
  origin: InvoiceOrigin; // "ManualUpload", "Email", "ManualEntry"
  originalFilePath: string;
  processedFilePath?: string;
  extractionConfidence?: number; // Confianza de extracción IA (0-1)
  notes?: string;
  
  // Líneas de detalle
  lines: InvoiceLine[];
  
  // Timestamps
  createdAt: Date;
  updatedAt: Date;
}

/**
 * Tipo de pago de una factura recibida.
 */
export enum PaymentType {
  Bank = 'Bank',
  Cash = 'Cash'
}

/**
 * Origen de la factura recibida.
 */
export enum InvoiceOrigin {
  ManualUpload = 'ManualUpload',
  Email = 'Email',
  ManualEntry = 'ManualEntry'
}

/**
 * Pago bancario asociado a una factura.
 */
export interface InvoicePayment {
  id: string;
  bankTransactionId: string;
  amount: number;
  currency: string;
  paymentDate: Date;
}

/**
 * Línea de detalle de una factura.
 */
export interface InvoiceLine {
  id: string;
  lineNumber: number;
  description: string;
  quantity: number;
  unitPrice: number;
  unitPriceCurrency: string;
  taxRate?: number;
  lineTotal: number;
  lineTotalCurrency: string;
}

/**
 * @deprecated Ya no se usa. Las facturas están contabilizadas desde su recepción.
 * Mantenido para compatibilidad temporal.
 */
export enum InvoiceReceivedStatus {
  Processed = 'Processed',
  Reviewed = 'Reviewed',
  Error = 'Error'
}

/**
 * @deprecated Usar InvoiceReceivedStatus. Mantenido para compatibilidad.
 */
export enum InvoiceStatus {
  Pending = 0,
  Paid = 1,
  Overdue = 2,
  Cancelled = 3
}

/**
 * @deprecated Usar Invoice.totalAmount. Mantenido para compatibilidad.
 */
export interface Money {
  value: number;
  currency: string;
}
