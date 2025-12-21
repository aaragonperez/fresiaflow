import { TaskItem, TaskPriority } from '../domain/task.model';

/**
 * Puerto (Port) para comunicación con la API de tareas.
 * Define el contrato que los adapters HTTP deben cumplir.
 */
export interface TaskApiPort {
  getPendingTasks(date?: Date): Promise<TaskItem[]>;
  getTaskById(id: string): Promise<TaskItem>;
  createTask(task: { title: string; description?: string; priority: TaskPriority }): Promise<TaskItem>;
  completeTask(taskId: string): Promise<void>;
  updateTaskPriority(taskId: string, priority: TaskPriority): Promise<void>;
  deleteTask(taskId: string): Promise<void>;
}

