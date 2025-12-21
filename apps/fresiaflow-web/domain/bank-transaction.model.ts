/**
 * Modelo de dominio para transacciones bancarias.
 */
export interface BankTransaction {
  id: string;
  bankAccountId: string;
  transactionDate: Date;
  amount: Money;
  description: string;
  reference?: string;
  externalTransactionId?: string;
  isReconciled: boolean;
  reconciledWithInvoiceId?: string;
  createdAt: Date;
}

export interface Money {
  value: number;
  currency: string;
}

