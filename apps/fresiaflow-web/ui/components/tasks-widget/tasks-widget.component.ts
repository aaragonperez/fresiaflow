import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardTask, DashboardTaskType, TaskPriority, TaskStatus } from '../../../domain/dashboard.model';

/**
 * Componente widget para mostrar tareas pendientes del dashboard.
 * Muestra una lista priorizada de tareas con su información relevante.
 */
@Component({
  selector: 'app-tasks-widget',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tasks-widget.component.html',
  styleUrls: ['./tasks-widget.component.css']
})
export class TasksWidgetComponent {
  @Input() tasks: DashboardTask[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;
  @Output() taskClick = new EventEmitter<DashboardTask>();

  /**
   * Maneja el clic en una tarea.
   */
  onTaskClick(task: DashboardTask): void {
    this.taskClick.emit(task);
  }

  // Enums para usar en el template
  readonly TaskType = DashboardTaskType;
  readonly TaskPriority = TaskPriority;
  readonly TaskStatus = TaskStatus;

  /**
   * Obtiene el icono según el tipo de tarea.
   */
  getTaskTypeIcon(type: DashboardTaskType): string {
    switch (type) {
      case DashboardTaskType.Invoice:
        return 'pi pi-file';
      case DashboardTaskType.Bank:
        return 'pi pi-wallet';
      case DashboardTaskType.Supplier:
        return 'pi pi-building';
      case DashboardTaskType.System:
        return 'pi pi-cog';
      case DashboardTaskType.Review:
        return 'pi pi-check-circle';
      default:
        return 'pi pi-circle';
    }
  }

  /**
   * Obtiene la clase CSS según la prioridad.
   */
  getPriorityClass(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.High:
        return 'priority-high';
      case TaskPriority.Medium:
        return 'priority-medium';
      case TaskPriority.Low:
        return 'priority-low';
      default:
        return '';
    }
  }

  /**
   * Obtiene el texto de la prioridad en español.
   */
  getPriorityText(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.High:
        return 'Alta';
      case TaskPriority.Medium:
        return 'Media';
      case TaskPriority.Low:
        return 'Baja';
      default:
        return priority;
    }
  }

  /**
   * Obtiene el texto del tipo de tarea en español.
   */
  getTaskTypeText(type: DashboardTaskType): string {
    switch (type) {
      case DashboardTaskType.Invoice:
        return 'Factura';
      case DashboardTaskType.Bank:
        return 'Banco';
      case DashboardTaskType.Supplier:
        return 'Proveedor';
      case DashboardTaskType.System:
        return 'Sistema';
      case DashboardTaskType.Review:
        return 'Revisión';
      default:
        return type;
    }
  }

  /**
   * Verifica si una tarea está vencida.
   */
  isOverdue(task: DashboardTask): boolean {
    if (!task.dueDate || task.status === TaskStatus.Completed) {
      return false;
    }
    return new Date(task.dueDate) < new Date();
  }

  /**
   * Formatea una fecha para mostrar.
   */
  formatDate(date: Date | undefined): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short' });
  }
}

