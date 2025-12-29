import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { DashboardFacade } from '../../../application/dashboard.facade';
import { TasksWidgetComponent } from '../../components/tasks-widget/tasks-widget.component';
import { BankSummaryWidgetComponent } from '../../components/bank-summary-widget/bank-summary-widget.component';
import { AlertsWidgetComponent } from '../../components/alerts-widget/alerts-widget.component';
import { DashboardTask, DashboardTaskType, TaskPriority } from '../../../domain/dashboard.model';

/**
 * Componente principal del Dashboard.
 * Contenedor que organiza los widgets principales del dashboard.
 */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    ToastModule,
    TasksWidgetComponent,
    BankSummaryWidgetComponent,
    AlertsWidgetComponent
  ],
  providers: [MessageService],
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.css']
})
export class DashboardPageComponent implements OnInit {
  private router = inject(Router);
  private messageService = inject(MessageService);
  facade = inject(DashboardFacade);

  // Exponer signals del facade
  tasks = this.facade.tasks;
  tasksLoading = this.facade.tasksLoading;
  tasksError = this.facade.tasksError;

  bankSummary = this.facade.bankSummary;
  bankSummaryLoading = this.facade.bankSummaryLoading;
  bankSummaryError = this.facade.bankSummaryError;

  alerts = this.facade.alerts;
  alertsLoading = this.facade.alertsLoading;
  alertsError = this.facade.alertsError;

  // Computed signals
  highPriorityTasks = this.facade.highPriorityTasks;
  pendingTasks = this.facade.pendingTasks;
  criticalAlerts = this.facade.criticalAlerts;
  unacknowledgedAlerts = this.facade.unacknowledgedAlerts;

  ngOnInit(): void {
    // Cargar todos los datos del dashboard al inicializar
    this.facade.loadAll();
  }

  /**
   * Recarga todos los datos del dashboard.
   */
  async refresh(): Promise<void> {
    await this.facade.loadAll();
  }

  /**
   * Maneja el clic en una tarea.
   * Si es una tarea de factura, navega a la pantalla de facturas con el ID a editar.
   */
  onTaskClick(task: DashboardTask): void {
    if (task.type === DashboardTaskType.Invoice && task.metadata?.['invoiceId']) {
      // Navegar a facturas con el parámetro de la factura a editar
      this.router.navigate(['/invoices'], { 
        queryParams: { editInvoiceId: task.metadata['invoiceId'] }
      });
    }
  }

  /**
   * Marca una tarea como completada.
   */
  async onTaskComplete(task: DashboardTask): Promise<void> {
    try {
      await this.facade.completeTask(task.id);
      this.messageService.add({
        severity: 'success',
        summary: 'Tarea completada',
        detail: task.title,
        life: 2000
      });
    } catch (error: any) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: error.message || 'No se pudo completar la tarea'
      });
    }
  }

  /**
   * Desmarca una tarea como completada.
   */
  async onTaskUncomplete(task: DashboardTask): Promise<void> {
    try {
      await this.facade.uncompleteTask(task.id);
      this.messageService.add({
        severity: 'info',
        summary: 'Tarea reabierta',
        detail: task.title,
        life: 2000
      });
    } catch (error: any) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: error.message || 'No se pudo reabrir la tarea'
      });
    }
  }

  /**
   * Cambia la prioridad de una tarea.
   */
  async onTaskPriorityChange(event: { task: DashboardTask; priority: TaskPriority }): Promise<void> {
    try {
      await this.facade.updateTaskPriority(event.task.id, event.priority);
      const priorityLabel = event.priority === TaskPriority.High ? 'Alta' : 
                            event.priority === TaskPriority.Medium ? 'Media' : 'Baja';
      this.messageService.add({
        severity: 'info',
        summary: 'Prioridad actualizada',
        detail: `${event.task.title} → ${priorityLabel}`,
        life: 2000
      });
    } catch (error: any) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: error.message || 'No se pudo actualizar la prioridad'
      });
    }
  }

  /**
   * Fija o desfija una tarea.
   */
  async onTaskPinToggle(task: DashboardTask): Promise<void> {
    try {
      await this.facade.toggleTaskPin(task.id);
      this.messageService.add({
        severity: 'info',
        summary: task.isPinned ? 'Tarea desfijada' : 'Tarea fijada',
        detail: task.title,
        life: 2000
      });
    } catch (error: any) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: error.message || 'No se pudo fijar/desfijar la tarea'
      });
    }
  }
}

