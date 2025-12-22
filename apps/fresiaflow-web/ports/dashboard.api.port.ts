import { DashboardTask } from '../domain/dashboard.model';
import { BankSummary } from '../domain/dashboard.model';
import { Alert } from '../domain/dashboard.model';

/**
 * Puerto (Port) para comunicación con la API del Dashboard.
 * Define el contrato para obtener datos del dashboard.
 */
export interface DashboardApiPort {
  /**
   * Obtiene todas las tareas pendientes del dashboard.
   * @returns Lista de tareas ordenadas por prioridad y fecha límite.
   */
  getTasks(): Promise<DashboardTask[]>;

  /**
   * Obtiene el resumen de saldos bancarios.
   * @returns Resumen con lista de bancos y totales agregados.
   */
  getBankBalances(): Promise<BankSummary>;

  /**
   * Obtiene todas las alertas activas.
   * @returns Lista de alertas ordenadas por severidad y fecha.
   */
  getAlerts(): Promise<Alert[]>;
}

