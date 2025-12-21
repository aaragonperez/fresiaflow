import { Provider, InjectionToken } from '@angular/core';
import { TaskApiPort } from '../../ports/task.api.port';
import { InvoiceApiPort } from '../../ports/invoice.api.port';
import { TaskHttpAdapter } from './task.http.adapter';
import { InvoiceHttpAdapter } from './invoice.http.adapter';

/**
 * Tokens de inyección para los puertos.
 */
export const TASK_API_PORT = new InjectionToken<TaskApiPort>('TaskApiPort');
export const INVOICE_API_PORT = new InjectionToken<InvoiceApiPort>('InvoiceApiPort');

/**
 * Providers para inyección de dependencias en Angular.
 * Conecta puertos con sus adapters.
 */
export const FRESIAFLOW_PROVIDERS: Provider[] = [
  { provide: TASK_API_PORT, useClass: TaskHttpAdapter },
  { provide: INVOICE_API_PORT, useClass: InvoiceHttpAdapter }
];

