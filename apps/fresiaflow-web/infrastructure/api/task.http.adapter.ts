import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TaskApiPort } from '../../ports/task.api.port';
import { TaskItem, TaskPriority } from '../../domain/task.model';
import { firstValueFrom } from 'rxjs';

/**
 * Adapter HTTP para el puerto de tareas.
 * Implementa la comunicación con el backend.
 */
@Injectable({ providedIn: 'root' })
export class TaskHttpAdapter implements TaskApiPort {
  private readonly baseUrl = '/api/tasks';

  constructor(private http: HttpClient) {}

  async getPendingTasks(date?: Date): Promise<TaskItem[]> {
    const options: any = {};
    if (date) {
      options.params = { date: date.toISOString() };
    }
    const response = await firstValueFrom(
      this.http.get<TaskItem[]>(`${this.baseUrl}/pending`, options)
    );
    return Array.isArray(response) ? response.map(this.mapToDomain) : [];
  }

  async getTaskById(id: string): Promise<TaskItem> {
    const response = await firstValueFrom(
      this.http.get<TaskItem>(`${this.baseUrl}/${id}`)
    );
    return this.mapToDomain(response);
  }

  async createTask(task: { title: string; description?: string; priority: TaskPriority }): Promise<TaskItem> {
    const response = await firstValueFrom(
      this.http.post<TaskItem>(this.baseUrl, task)
    );
    return this.mapToDomain(response);
  }

  async completeTask(taskId: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/${taskId}/complete`, {})
    );
  }

  async updateTaskPriority(taskId: string, priority: TaskPriority): Promise<void> {
    await firstValueFrom(
      this.http.patch<void>(`${this.baseUrl}/${taskId}/priority`, { priority })
    );
  }

  async deleteTask(taskId: string): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${taskId}`)
    );
  }

  private mapToDomain(dto: any): TaskItem {
    return {
      ...dto,
      createdAt: new Date(dto.createdAt),
      dueDate: dto.dueDate ? new Date(dto.dueDate) : undefined,
      completedAt: dto.completedAt ? new Date(dto.completedAt) : undefined
    };
  }
}

