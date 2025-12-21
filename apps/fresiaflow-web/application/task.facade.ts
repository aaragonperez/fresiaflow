import { Injectable, signal, computed, Inject } from '@angular/core';
import { TaskItem, TaskPriority } from '../domain/task.model';
import { TaskApiPort } from '../ports/task.api.port';
import { TASK_API_PORT } from '../infrastructure/api/providers';

/**
 * Facade para gestión de tareas.
 * Gestiona el estado usando signals y delega a los puertos.
 */
@Injectable({ providedIn: 'root' })
export class TaskFacade {
  private _tasks = signal<TaskItem[]>([]);
  private _loading = signal<boolean>(false);
  private _error = signal<string | null>(null);

  // Estado público (readonly)
  readonly tasks = this._tasks.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // Computed signals
  readonly pendingTasks = computed(() =>
    this._tasks().filter(t => !t.isCompleted)
  );

  readonly completedTasks = computed(() =>
    this._tasks().filter(t => t.isCompleted)
  );

  readonly urgentTasks = computed(() =>
    this._tasks().filter(t => t.priority === TaskPriority.Urgent && !t.isCompleted)
  );

  constructor(@Inject(TASK_API_PORT) private taskApi: TaskApiPort) {}

  async loadTasks(date?: Date): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const tasks = await this.taskApi.getPendingTasks(date);
      this._tasks.set(tasks);
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al cargar tareas');
    } finally {
      this._loading.set(false);
    }
  }

  async createTask(title: string, description?: string, priority: TaskPriority = TaskPriority.Medium): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const newTask = await this.taskApi.createTask({ title, description, priority });
      this._tasks.update(tasks => [...tasks, newTask]);
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al crear tarea');
    } finally {
      this._loading.set(false);
    }
  }

  async completeTask(taskId: string): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      await this.taskApi.completeTask(taskId);
      this._tasks.update(tasks =>
        tasks.map(t => t.id === taskId ? { ...t, isCompleted: true, completedAt: new Date() } : t)
      );
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al completar tarea');
    } finally {
      this._loading.set(false);
    }
  }

  async updateTaskPriority(taskId: string, priority: TaskPriority): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      await this.taskApi.updateTaskPriority(taskId, priority);
      this._tasks.update(tasks =>
        tasks.map(t => t.id === taskId ? { ...t, priority } : t)
      );
    } catch (error: unknown) {
      this._error.set(error instanceof Error ? error.message : 'Error al actualizar prioridad');
    } finally {
      this._loading.set(false);
    }
  }
}

