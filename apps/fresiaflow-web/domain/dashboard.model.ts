/**
 * Modelos de dominio para el Dashboard.
 * Representan la información que se muestra en el dashboard principal.
 */

/**
 * Tipo de tarea del dashboard.
 */
export enum DashboardTaskType {
  Invoice = 'invoice',
  Bank = 'bank',
  Supplier = 'supplier',
  System = 'system',
  Review = 'review'
}

/**
 * Prioridad de una tarea.
 */
export enum TaskPriority {
  High = 'high',
  Medium = 'medium',
  Low = 'low'
}

/**
 * Estado de una tarea.
 */
export enum TaskStatus {
  Pending = 'pending',
  InProgress = 'in_progress',
  Completed = 'completed',
  Cancelled = 'cancelled'
}

/**
 * Tarea del dashboard.
 * Representa una acción pendiente o requerida en el sistema.
 */
export interface DashboardTask {
  id: string;
  title: string;
  description?: string;
  type: DashboardTaskType;
  priority: TaskPriority;
  status: TaskStatus;
  isPinned: boolean;
  dueDate?: Date;
  createdAt: Date;
  updatedAt: Date;
  metadata?: Record<string, any>; // Para datos adicionales específicos del tipo de tarea
}

/**
 * Saldo de un banco.
 */
export interface BankBalance {
  bankId: string;
  bankName: string;
  accountNumber?: string;
  balance: number;
  currency: string;
  lastMovementDate?: Date;
  lastMovementAmount?: number;
}

/**
 * Resumen de saldos bancarios.
 */
export interface BankSummary {
  banks: BankBalance[];
  totalBalance: number;
  primaryCurrency: string;
  previousDayBalance?: number;
  previousDayVariation?: number;
  previousMonthBalance?: number;
  previousMonthVariation?: number;
}

/**
 * Nivel de severidad de una alerta.
 */
export enum AlertSeverity {
  Critical = 'critical',
  High = 'high',
  Medium = 'medium',
  Low = 'low',
  Info = 'info'
}

/**
 * Tipo de alerta financiera.
 */
export enum AlertType {
  UnusualMovement = 'unusual_movement',
  DuplicateAmount = 'duplicate_amount',
  UnidentifiedCharge = 'unidentified_charge',
  PatternDeviation = 'pattern_deviation',
  MissingSupplier = 'missing_supplier',
  OverdueInvoice = 'overdue_invoice',
  LowBalance = 'low_balance',
  System = 'system'
}

/**
 * Alerta financiera o del sistema.
 */
export interface Alert {
  id: string;
  type: AlertType;
  severity: AlertSeverity;
  title: string;
  description: string;
  occurredAt: Date;
  acknowledgedAt?: Date;
  resolvedAt?: Date;
  metadata?: Record<string, any>; // Para datos adicionales específicos del tipo de alerta
}

