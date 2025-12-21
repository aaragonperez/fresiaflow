/**
 * Modelo de dominio para tareas.
 */
export interface TaskItem {
  id: string;
  title: string;
  description?: string;
  priority: TaskPriority;
  isCompleted: boolean;
  dueDate?: Date;
  createdAt: Date;
  completedAt?: Date;
  relatedInvoiceId?: string;
  relatedTransactionId?: string;
}

export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Urgent = 3
}

