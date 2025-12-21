import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskFacade } from '../../../application/task.facade';
import { TaskItem, TaskPriority } from '../../../domain/task.model';

/**
 * Componente de página para gestión de tareas.
 * Solo maneja UI; delega lógica a facades.
 */
@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tasks-page.component.html',
  styleUrls: ['./tasks-page.component.css']
})
export class TasksPageComponent implements OnInit {
  facade = inject(TaskFacade);
  
  tasks = this.facade.tasks;
  loading = this.facade.loading;
  error = this.facade.error;
  pendingTasks = this.facade.pendingTasks;
  urgentTasks = this.facade.urgentTasks;

  newTaskTitle = '';
  newTaskDescription = '';
  newTaskPriority: TaskPriority = TaskPriority.Medium;

  TaskPriority = TaskPriority;

  ngOnInit(): void {
    this.facade.loadTasks();
  }

  async createTask(): Promise<void> {
    if (!this.newTaskTitle.trim()) return;

    await this.facade.createTask(
      this.newTaskTitle,
      this.newTaskDescription || undefined,
      this.newTaskPriority
    );

    this.newTaskTitle = '';
    this.newTaskDescription = '';
    this.newTaskPriority = TaskPriority.Medium;
  }

  async completeTask(taskId: string): Promise<void> {
    await this.facade.completeTask(taskId);
  }

  async updatePriority(taskId: string, priority: TaskPriority): Promise<void> {
    await this.facade.updateTaskPriority(taskId, priority);
  }
}

