import { Component, Input, Output, EventEmitter, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { DropdownModule } from 'primeng/dropdown';
import { PaginatorModule } from 'primeng/paginator';
import { DashboardTask, DashboardTaskType, TaskPriority, TaskStatus } from '../../../domain/dashboard.model';

/**
 * Componente widget para mostrar tareas pendientes del dashboard.
 * Muestra una lista priorizada de tareas con su información relevante.
 */
@Component({
  selector: 'app-tasks-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, TooltipModule, DropdownModule, PaginatorModule],
  templateUrl: './tasks-widget.component.html',
  styleUrls: ['./tasks-widget.component.css']
})
export class TasksWidgetComponent {
  @Input() tasks: DashboardTask[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;
  @Input() showCompleted = false;
  
  @Output() taskClick = new EventEmitter<DashboardTask>();
  @Output() taskComplete = new EventEmitter<DashboardTask>();
  @Output() taskUncomplete = new EventEmitter<DashboardTask>();
  @Output() taskPriorityChange = new EventEmitter<{ task: DashboardTask; priority: TaskPriority }>();
  @Output() taskPinToggle = new EventEmitter<DashboardTask>();

  // Filtro de prioridad
  priorityFilter = signal<TaskPriority | 'all'>('all');
  
  // Paginación
  currentPage = signal(0);
  rowsPerPage = signal(5);
  rowsPerPageOptions = [5, 10, 20, 50];
  
  // Opciones de filtro de prioridad
  priorityOptions = [
    { label: 'Todas', value: 'all' },
    { label: 'Alta', value: TaskPriority.High },
    { label: 'Media', value: TaskPriority.Medium },
    { label: 'Baja', value: TaskPriority.Low }
  ];

  // Opciones de prioridad para el dropdown de cambio
  priorityChangeOptions = [
    { label: 'Alta', value: TaskPriority.High },
    { label: 'Media', value: TaskPriority.Medium },
    { label: 'Baja', value: TaskPriority.Low }
  ];

  /**
   * Obtiene las tareas filtradas y ordenadas.
   * Orden: Fijadas primero, luego por prioridad, luego por fecha.
   */
  get filteredTasks(): DashboardTask[] {
    let filtered = this.tasks;

    // Filtrar por estado completado
    if (!this.showCompleted) {
      filtered = filtered.filter(t => t.status !== TaskStatus.Completed);
    }

    // Filtrar por prioridad
    const pf = this.priorityFilter();
    if (pf !== 'all') {
      filtered = filtered.filter(t => t.priority === pf);
    }

    // Ordenar: fijadas primero, luego por prioridad, luego por fecha
    return [...filtered].sort((a, b) => {
      // Fijadas primero
      if (a.isPinned && !b.isPinned) return -1;
      if (!a.isPinned && b.isPinned) return 1;
      
      // Por prioridad (High > Medium > Low)
      const priorityOrder = { high: 0, medium: 1, low: 2 };
      const priorityDiff = priorityOrder[a.priority] - priorityOrder[b.priority];
      if (priorityDiff !== 0) return priorityDiff;
      
      // Por fecha de vencimiento
      if (a.dueDate && b.dueDate) {
        return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
      }
      if (a.dueDate) return -1;
      if (b.dueDate) return 1;
      
      return 0;
    });
  }

  /**
   * Obtiene las tareas paginadas.
   */
  get paginatedTasks(): DashboardTask[] {
    const filtered = this.filteredTasks;
    const start = this.currentPage() * this.rowsPerPage();
    const end = start + this.rowsPerPage();
    return filtered.slice(start, end);
  }

  /**
   * Total de tareas filtradas (para el paginador).
   */
  get totalFilteredTasks(): number {
    return this.filteredTasks.length;
  }

  /**
   * Cuenta de tareas por estado.
   */
  get pendingCount(): number {
    return this.tasks.filter(t => t.status !== TaskStatus.Completed).length;
  }

  get completedCount(): number {
    return this.tasks.filter(t => t.status === TaskStatus.Completed).length;
  }

  /**
   * Maneja el clic en una tarea.
   */
  onTaskClick(task: DashboardTask): void {
    this.taskClick.emit(task);
  }

  /**
   * Marca/desmarca una tarea como completada.
   */
  onToggleComplete(task: DashboardTask, event: Event): void {
    event.stopPropagation();
    if (task.status === TaskStatus.Completed) {
      this.taskUncomplete.emit(task);
    } else {
      this.taskComplete.emit(task);
    }
  }

  /**
   * Cambia la prioridad de una tarea.
   */
  onPriorityChange(task: DashboardTask, priority: TaskPriority, event: Event): void {
    event.stopPropagation();
    if (task.priority !== priority) {
      this.taskPriorityChange.emit({ task, priority });
    }
  }

  /**
   * Fija/desfija una tarea.
   */
  onTogglePin(task: DashboardTask, event: Event): void {
    event.stopPropagation();
    this.taskPinToggle.emit(task);
  }

  /**
   * Cambia el filtro de prioridad.
   */
  onPriorityFilterChange(value: TaskPriority | 'all'): void {
    this.priorityFilter.set(value);
    this.currentPage.set(0); // Resetear a la primera página al cambiar el filtro
  }

  /**
   * Maneja el cambio de página en el paginador.
   */
  onPageChange(event: any): void {
    this.currentPage.set(event.page);
    this.rowsPerPage.set(event.rows);
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

