import { Injectable, signal, computed, Inject } from '@angular/core';
import { DashboardTask, BankSummary, Alert } from '../domain/dashboard.model';
import { DashboardApiPort } from '../ports/dashboard.api.port';
import { DASHBOARD_API_PORT } from '../infrastructure/api/providers';

/**
 * Facade para gestión del Dashboard.
 * Gestiona el estado usando signals y proporciona métodos para cargar datos.
 */
@Injectable({ providedIn: 'root' })
export class DashboardFacade {
  // Estado de tareas
  private _tasks = signal<DashboardTask[]>([]);
  private _tasksLoading = signal<boolean>(false);
  private _tasksError = signal<string | null>(null);

  // Estado de saldos bancarios
  private _bankSummary = signal<BankSummary | null>(null);
  private _bankSummaryLoading = signal<boolean>(false);
  private _bankSummaryError = signal<string | null>(null);

  // Estado de alertas
  private _alerts = signal<Alert[]>([]);
  private _alertsLoading = signal<boolean>(false);
  private _alertsError = signal<string | null>(null);

  // Readonly signals para componentes
  readonly tasks = this._tasks.asReadonly();
  readonly tasksLoading = this._tasksLoading.asReadonly();
  readonly tasksError = this._tasksError.asReadonly();

  readonly bankSummary = this._bankSummary.asReadonly();
  readonly bankSummaryLoading = this._bankSummaryLoading.asReadonly();
  readonly bankSummaryError = this._bankSummaryError.asReadonly();

  readonly alerts = this._alerts.asReadonly();
  readonly alertsLoading = this._alertsLoading.asReadonly();
  readonly alertsError = this._alertsError.asReadonly();

  // Computed signals para datos derivados
  readonly highPriorityTasks = computed(() =>
    this._tasks().filter(t => t.priority === 'high' && t.status !== 'completed')
  );

  readonly pendingTasks = computed(() =>
    this._tasks().filter(t => t.status === 'pending')
  );

  readonly criticalAlerts = computed(() =>
    this._alerts().filter(a => a.severity === 'critical' && !a.resolvedAt)
  );

  readonly unacknowledgedAlerts = computed(() =>
    this._alerts().filter(a => !a.acknowledgedAt && !a.resolvedAt)
  );

  constructor(@Inject(DASHBOARD_API_PORT) private dashboardApi: DashboardApiPort) {}

  /**
   * Carga todas las tareas del dashboard.
   */
  async loadTasks(): Promise<void> {
    this._tasksLoading.set(true);
    this._tasksError.set(null);

    try {
      const tasks = await this.dashboardApi.getTasks();
      this._tasks.set(tasks);
    } catch (error: unknown) {
      this._tasksError.set(error instanceof Error ? error.message : 'Error al cargar tareas');
      this._tasks.set([]); // Reset en caso de error
    } finally {
      this._tasksLoading.set(false);
    }
  }

  /**
   * Carga el resumen de saldos bancarios.
   */
  async loadBankSummary(): Promise<void> {
    this._bankSummaryLoading.set(true);
    this._bankSummaryError.set(null);

    try {
      const summary = await this.dashboardApi.getBankBalances();
      this._bankSummary.set(summary);
    } catch (error: unknown) {
      this._bankSummaryError.set(error instanceof Error ? error.message : 'Error al cargar saldos bancarios');
      this._bankSummary.set(null);
    } finally {
      this._bankSummaryLoading.set(false);
    }
  }

  /**
   * Carga todas las alertas.
   */
  async loadAlerts(): Promise<void> {
    this._alertsLoading.set(true);
    this._alertsError.set(null);

    try {
      const alerts = await this.dashboardApi.getAlerts();
      this._alerts.set(alerts);
    } catch (error: unknown) {
      this._alertsError.set(error instanceof Error ? error.message : 'Error al cargar alertas');
      this._alerts.set([]); // Reset en caso de error
    } finally {
      this._alertsLoading.set(false);
    }
  }

  /**
   * Carga todos los datos del dashboard.
   */
  async loadAll(): Promise<void> {
    await Promise.all([
      this.loadTasks(),
      this.loadBankSummary(),
      this.loadAlerts()
    ]);
  }
}

