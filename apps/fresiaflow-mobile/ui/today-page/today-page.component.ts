import { Component, OnInit, inject } from '@angular/core';
// TODO: Importar TaskFacade desde web o crear uno específico para mobile
// import { TaskFacade } from '../../application/task.facade';

/**
 * Página principal de la app móvil: vista del día actual.
 */
@Component({
  selector: 'app-today-page',
  standalone: true,
  templateUrl: './today-page.component.html',
  styleUrls: ['./today-page.component.css']
})
export class TodayPageComponent implements OnInit {
  // TODO: Inyectar TaskFacade cuando esté disponible
  // facade = inject(TaskFacade);
  // tasks = this.facade.pendingTasks;
  // loading = this.facade.loading;
  tasks: any[] = [];
  loading = false;

  ngOnInit(): void {
    // TODO: Cargar tareas cuando el facade esté disponible
    // this.facade.loadTasks(new Date());
  }
}

