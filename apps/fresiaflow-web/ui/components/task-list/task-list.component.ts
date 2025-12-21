import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskItem } from '../../../domain/task.model';

/**
 * Componente presentacional para lista de tareas.
 * Solo recibe inputs y emite outputs.
 */
@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.css']
})
export class TaskListComponent {
  @Input() tasks: TaskItem[] = [];
  @Input() showCompleted = false;
}

